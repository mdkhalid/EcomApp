import { Component, inject, signal, Output, EventEmitter, OnInit, OnDestroy, ElementRef, ViewChild, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Subject, debounceTime, distinctUntilChanged, switchMap, takeUntil, of } from 'rxjs';
import { ProductService } from '../../services/product.service';
import { SearchSuggestion } from '../../models/product.model';

@Component({
  selector: 'app-search-bar',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './search-bar.component.html',
  styleUrl: './search-bar.component.scss'
})
export class SearchBarComponent implements OnInit, OnDestroy {
  @Output() searchChanged = new EventEmitter<string>();
  @Output() searchSubmitted = new EventEmitter<string>();
  @ViewChild('searchInput') searchInput!: ElementRef<HTMLInputElement>;

  private readonly productService = inject(ProductService);
  private readonly router = inject(Router);
  private readonly destroy$ = new Subject<void>();
  private readonly searchSubject = new Subject<string>();

  searchTerm = signal('');
  suggestions = signal<SearchSuggestion>({ suggestions: [], recentSearches: [], popularCategories: [] });
  showSuggestions = signal(false);
  isLoadingSuggestions = signal(false);
  selectedIndex = signal(-1);
  recentSearches = signal<string[]>([]);

  ngOnInit(): void {
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
        this.selectedIndex.set(-1);
      },
      error: () => {
        this.isLoadingSuggestions.set(false);
      }
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: Event): void {
    const target = event.target as HTMLElement;
    if (!target.closest('.search-container')) {
      this.showSuggestions.set(false);
    }
  }

  onInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.searchTerm.set(value);
    this.searchSubject.next(value);
  }

  onFocus(): void {
    if (this.searchTerm().length >= 2) {
      this.showSuggestions.set(true);
    }
  }

  onKeyDown(event: KeyboardEvent): void {
    const suggestions = this.getFlattenedSuggestions();
    const maxIndex = suggestions.length - 1;

    switch (event.key) {
      case 'ArrowDown':
        event.preventDefault();
        this.selectedIndex.update(i => Math.min(i + 1, maxIndex));
        break;
      case 'ArrowUp':
        event.preventDefault();
        this.selectedIndex.update(i => Math.max(i - 1, -1));
        break;
      case 'Enter':
        event.preventDefault();
        if (this.selectedIndex() >= 0 && this.selectedIndex() < suggestions.length) {
          this.selectSuggestion(suggestions[this.selectedIndex()]);
        } else {
          this.submitSearch();
        }
        break;
      case 'Escape':
        this.showSuggestions.set(false);
        this.selectedIndex.set(-1);
        break;
    }
  }

  getFlattenedSuggestions(): string[] {
    const result: string[] = [];
    const s = this.suggestions();

    // Recent searches first
    if (s.recentSearches?.length) {
      result.push(...s.recentSearches.slice(0, 3));
    }

    // Product suggestions
    if (s.suggestions?.length) {
      result.push(...s.suggestions.slice(0, 5));
    }

    // Popular categories
    if (s.popularCategories?.length) {
      result.push(...s.popularCategories.slice(0, 3));
    }

    return [...new Set(result)];
  }

  selectSuggestion(suggestion: string): void {
    this.searchTerm.set(suggestion);
    this.showSuggestions.set(false);
    this.saveRecentSearch(suggestion);
    this.searchSubmitted.emit(suggestion);
    this.router.navigate(['/products'], { queryParams: { search: suggestion } });
  }

  submitSearch(): void {
    const term = this.searchTerm().trim();
    if (term) {
      this.saveRecentSearch(term);
    }
    this.showSuggestions.set(false);
    this.searchSubmitted.emit(term);
    this.router.navigate(['/products'], { queryParams: { search: term || undefined } });
  }

  clearSearch(): void {
    this.searchTerm.set('');
    this.showSuggestions.set(false);
    this.searchChanged.emit('');
  }

  removeRecentSearch(search: string, event: Event): void {
    event.stopPropagation();
    const searches = this.recentSearches().filter(s => s !== search);
    this.recentSearches.set(searches);
    this.saveRecentSearches();
  }

  private loadRecentSearches(): void {
    try {
      const stored = localStorage.getItem('recentSearches');
      if (stored) {
        this.recentSearches.set(JSON.parse(stored));
      }
    } catch {
      this.recentSearches.set([]);
    }
  }

  private saveRecentSearch(search: string): void {
    const searches = this.recentSearches();
    const updated = [search, ...searches.filter(s => s !== search)].slice(0, 5);
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

  highlightMatch(text: string, query: string): string {
    if (!query) return text;
    const regex = new RegExp(`(${query.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')})`, 'gi');
    return text.replace(regex, '<strong>$1</strong>');
  }
}
