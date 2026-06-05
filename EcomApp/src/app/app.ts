import { Component, signal, computed, inject, OnInit, OnDestroy, HostListener, ElementRef, viewChild } from '@angular/core';
import { RouterLink, RouterOutlet, Router, NavigationEnd } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { toSignal } from '@angular/core/rxjs-interop';
import { Subject, debounceTime, distinctUntilChanged, switchMap, takeUntil, of, filter, map, startWith } from 'rxjs';
import { AuthService } from './services/auth.service';
import { CartService } from './services/cart.service';
import { CategoryService } from './services/category.service';
import { Category } from './models/category.model';
import { ProductService } from './services/product.service';
import { SearchSuggestion } from './models/product.model';

@Component({
  selector: 'app-root',
  imports: [RouterLink, RouterOutlet, FormsModule],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App implements OnInit, OnDestroy {
  protected readonly title = signal('ShopKart');
  protected readonly authService = inject(AuthService);
  protected readonly cartService = inject(CartService);
  protected readonly router = inject(Router);
  protected readonly categoryService = inject(CategoryService);
  protected readonly productService = inject(ProductService);
  protected categories = signal<Category[]>([]);
  protected darkMode = signal(false);
  protected readonly userMenuOpen = signal(false);

  // Search state
  protected searchQuery = signal('');
  protected suggestions = signal<SearchSuggestion>({ suggestions: [], recentSearches: [], popularCategories: [] });
  protected showSuggestions = signal(false);
  protected isLoadingSuggestions = signal(false);
  protected selectedSuggestionIndex = signal(-1);
  protected recentSearches = signal<string[]>([]);
  private readonly destroy$ = new Subject<void>();
  private readonly searchSubject = new Subject<string>();

  protected readonly currentUrl = toSignal(
    this.router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd),
      map((e) => e.urlAfterRedirects),
      startWith(this.router.url)
    ),
    { initialValue: this.router.url }
  );
  protected readonly isAdminRoute = computed(() => this.currentUrl().startsWith('/admin'));

  ngOnInit(): void {
    this.categoryService.getAll().subscribe(data => this.categories.set(data));
    if (!this.authService.isAdmin()) {
      this.cartService.getCart().subscribe();
    }
    const savedTheme = localStorage.getItem('theme');
    if (savedTheme === 'dark' || (!savedTheme && window.matchMedia('(prefers-color-scheme: dark)').matches)) {
      this.darkMode.set(true);
      document.body.classList.add('dark-mode');
    }

    this.loadRecentSearches();

    this.searchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      switchMap(query => {
        if (!query || query.length < 2) {
          return of({ suggestions: [], recentSearches: this.recentSearches(), popularCategories: [] });
        }
        this.isLoadingSuggestions.set(true);
        return this.productService.getSuggestions(query);
      }),
      takeUntil(this.destroy$)
    ).subscribe({
      next: (result) => {
        this.suggestions.set(result);
        this.isLoadingSuggestions.set(false);
        this.showSuggestions.set(true);
        this.selectedSuggestionIndex.set(-1);
      },
      error: () => this.isLoadingSuggestions.set(false)
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  protected toggleDarkMode(): void {
    this.darkMode.set(!this.darkMode());
    document.body.classList.add('theme-transition');
    if (this.darkMode()) {
      document.body.classList.add('dark-mode');
      localStorage.setItem('theme', 'dark');
    } else {
      document.body.classList.remove('dark-mode');
      localStorage.setItem('theme', 'light');
    }
    setTimeout(() => document.body.classList.remove('theme-transition'), 350);
  }

  protected onSearch(): void {
    const q = this.searchQuery().trim();
    if (q) this.saveRecentSearch(q);
    this.showSuggestions.set(false);
    if (q) {
      this.router.navigate(['/products'], { queryParams: { search: q } });
    }
  }

  protected onSearchInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.searchQuery.set(value);
    this.searchSubject.next(value);
  }

  protected onSearchFocus(): void {
    if (this.searchQuery().length >= 2) {
      this.showSuggestions.set(true);
    } else if (this.recentSearches().length > 0) {
      this.suggestions.set({ suggestions: [], recentSearches: this.recentSearches(), popularCategories: [] });
      this.showSuggestions.set(true);
    }
  }

  protected onSearchKeyDown(event: KeyboardEvent): void {
    const items = this.getFlattenedSuggestions();
    const maxIndex = items.length - 1;
    switch (event.key) {
      case 'ArrowDown':
        event.preventDefault();
        this.selectedSuggestionIndex.update(i => Math.min(i + 1, maxIndex));
        break;
      case 'ArrowUp':
        event.preventDefault();
        this.selectedSuggestionIndex.update(i => Math.max(i - 1, -1));
        break;
      case 'Enter':
        event.preventDefault();
        if (this.selectedSuggestionIndex() >= 0 && this.selectedSuggestionIndex() < items.length) {
          this.selectSuggestion(items[this.selectedSuggestionIndex()]);
        } else {
          this.onSearch();
        }
        break;
      case 'Escape':
        this.showSuggestions.set(false);
        this.selectedSuggestionIndex.set(-1);
        break;
    }
  }

  protected getFlattenedSuggestions(): string[] {
    const s = this.suggestions();
    const result: string[] = [];
    if (s.recentSearches?.length) result.push(...s.recentSearches.slice(0, 3));
    if (s.suggestions?.length) result.push(...s.suggestions.slice(0, 5));
    if (s.popularCategories?.length) result.push(...s.popularCategories.slice(0, 3));
    return [...new Set(result)];
  }

  protected selectSuggestion(suggestion: string): void {
    this.searchQuery.set(suggestion);
    this.showSuggestions.set(false);
    this.saveRecentSearch(suggestion);
    this.router.navigate(['/products'], { queryParams: { search: suggestion } });
  }

  protected clearSearch(): void {
    this.searchQuery.set('');
    this.showSuggestions.set(false);
    this.router.navigate(['/products']);
  }

  protected removeRecentSearch(search: string, event: Event): void {
    event.stopPropagation();
    const updated = this.recentSearches().filter(s => s !== search);
    this.recentSearches.set(updated);
    this.saveRecentSearches();
  }

  protected highlightMatch(text: string, query: string): string {
    if (!query) return text;
    const regex = new RegExp(`(${query.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')})`, 'gi');
    return text.replace(regex, '<strong>$1</strong>');
  }

  private loadRecentSearches(): void {
    try {
      const stored = localStorage.getItem('recentSearches');
      if (stored) this.recentSearches.set(JSON.parse(stored));
    } catch {
      this.recentSearches.set([]);
    }
  }

  private saveRecentSearch(search: string): void {
    const updated = [search, ...this.recentSearches().filter(s => s !== search)].slice(0, 5);
    this.recentSearches.set(updated);
    this.saveRecentSearches();
  }

  private saveRecentSearches(): void {
    try {
      localStorage.setItem('recentSearches', JSON.stringify(this.recentSearches()));
    } catch {
      // localStorage not available
    }
  }

  protected toggleUserMenu(): void {
    this.userMenuOpen.set(!this.userMenuOpen());
  }

  protected closeUserMenu(): void {
    this.userMenuOpen.set(false);
  }

  @HostListener('document:click', ['$event'])
  protected onDocumentClick(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    if (!target.closest('.header-user-dropdown')) {
      this.userMenuOpen.set(false);
    }
    if (!target.closest('.header-center .search-box')) {
      this.showSuggestions.set(false);
    }
  }

  @HostListener('document:keydown.escape')
  protected onEscape(): void {
    this.userMenuOpen.set(false);
    this.showSuggestions.set(false);
  }

  protected logout(): void {
    this.userMenuOpen.set(false);
    this.cartService.resetCount();
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
