import { Component, signal, inject, OnInit, HostListener } from '@angular/core';
import { RouterLink, RouterOutlet, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService } from './services/auth.service';
import { CartService } from './services/cart.service';
import { CategoryService } from './services/category.service';
import { Category } from './models/category.model';

@Component({
  selector: 'app-root',
  imports: [RouterLink, RouterOutlet, FormsModule],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App implements OnInit {
  protected readonly title = signal('ShopKart');
  protected readonly authService = inject(AuthService);
  protected readonly cartService = inject(CartService);
  protected readonly router = inject(Router);
  protected readonly categoryService = inject(CategoryService);
  protected searchQuery = signal('');
  protected categories = signal<Category[]>([]);
  protected darkMode = signal(false);
  protected readonly userMenuOpen = signal(false);

  ngOnInit(): void {
    this.categoryService.getAll().subscribe(data => this.categories.set(data));
    // Fetch cart count for non-admin users (admin carts throw 401 on backend)
    if (!this.authService.isAdmin()) {
      this.cartService.getCart().subscribe();
    }
    // Load dark mode preference
    const savedTheme = localStorage.getItem('theme');
    if (savedTheme === 'dark' || (!savedTheme && window.matchMedia('(prefers-color-scheme: dark)').matches)) {
      this.darkMode.set(true);
      document.body.classList.add('dark-mode');
    }
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
    if (q) {
      this.router.navigate(['/products'], { queryParams: { search: q } });
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
  }

  @HostListener('document:keydown.escape')
  protected onEscape(): void {
    this.userMenuOpen.set(false);
  }

  protected logout(): void {
    this.userMenuOpen.set(false);
    this.cartService.resetCount();
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
