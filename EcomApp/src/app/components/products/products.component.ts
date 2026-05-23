import { Component, inject, OnInit, OnDestroy, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ProductService, PaginatedResponse } from '../../services/product.service';
import { NotificationService } from '../../services/notification.service';
import { CartService } from '../../services/cart.service';
import { AuthService } from '../../services/auth.service';
import { WishlistService } from '../../services/wishlist.service';
import { CategoryService } from '../../services/category.service';
import { BannerService } from '../../services/banner.service';
import { Product } from '../../models/product.model';
import { Category } from '../../models/category.model';
import { Banner } from '../../models/banner.model';

@Component({
  selector: 'app-products',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './products.component.html',
  styleUrl: './products.component.scss'
})
export class ProductsComponent implements OnInit, OnDestroy {
  private readonly productService = inject(ProductService);
  private readonly cartService = inject(CartService);
  private readonly categoryService = inject(CategoryService);
  private readonly bannerService = inject(BannerService);
  readonly authService = inject(AuthService);
  readonly wishlistService = inject(WishlistService);
  readonly notification = inject(NotificationService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  protected readonly Math = Math;
  protected readonly notifications = this.notification.notifications;

  products = signal<Product[]>([]);
  totalCount = signal(0);
  pageNumber = signal(1);
  pageSize = signal(12);
  searchTerm = signal('');
  categoryFilter = signal('');
  sortBy = signal('');
  isLoading = signal(false);
  showMobileFilters = signal(false);
  minPrice: number | null = null;
  maxPrice: number | null = null;

  categories = signal<Category[]>([]);
  banners = signal<Banner[]>([]);
  currentBanner = signal(0);
  private bannerInterval: ReturnType<typeof setInterval> | null = null;
  bannerPaused = signal(false);

  totalPages = computed(() => Math.ceil(this.totalCount() / this.pageSize()));

  ngOnInit(): void {
    if (this.authService.isAdmin()) {
      this.router.navigate(['/admin']);
      return;
    }
    this.categoryService.getAll().subscribe(data => this.categories.set(data));
    this.bannerService.getActive().subscribe(data => {
      this.banners.set(data);
      if (data.length > 0) this.startBannerRotation();
    });
    this.route.queryParams.subscribe(params => {
      if (params['search']) {
        this.searchTerm.set(params['search']);
      }
      if (params['category']) {
        this.categoryFilter.set(params['category']);
      }
      this.loadProducts();
    });
    this.startBannerRotation();
  }

  ngOnDestroy(): void {
    this.stopBannerRotation();
  }

  startBannerRotation(): void {
    this.stopBannerRotation();
    const len = this.banners().length;
    if (len < 2) return;
    this.bannerInterval = setInterval(() => {
      if (!this.bannerPaused()) {
        this.currentBanner.update(i => (i + 1) % len);
      }
    }, 3500);
  }

  stopBannerRotation(): void {
    if (this.bannerInterval) {
      clearInterval(this.bannerInterval);
      this.bannerInterval = null;
    }
  }

  goToBanner(index: number): void {
    this.currentBanner.set(index);
  }

  loadProducts(): void {
    this.isLoading.set(true);
    this.productService.getAll({
      pageNumber: this.pageNumber(),
      pageSize: this.pageSize(),
      search: this.searchTerm() || undefined,
      category: this.categoryFilter() || undefined,
      minPrice: this.minPrice ?? undefined,
      maxPrice: this.maxPrice ?? undefined,
      sortBy: this.sortBy() || undefined
    }).subscribe({
      next: (data: PaginatedResponse<Product>) => {
        this.products.set(data.items);
        this.totalCount.set(data.totalCount);
        this.isLoading.set(false);
      },
      error: () => {
        this.notification.showError('Failed to load products.');
        this.isLoading.set(false);
      }
    });
  }

  onSearchInput(value: string): void {
    this.searchTerm.set(value);
  }

  onSortChange(value: string): void {
    this.sortBy.set(value);
    this.pageNumber.set(1);
    this.loadProducts();
  }

  onSearch(): void {
    this.pageNumber.set(1);
    this.router.navigate(['/products'], { queryParams: { search: this.searchTerm() || undefined, category: this.categoryFilter() || undefined } });
    this.loadProducts();
  }

  onCategoryChange(category: string): void {
    this.categoryFilter.set(category);
    this.pageNumber.set(1);
    this.router.navigate(['/products'], { queryParams: { search: this.searchTerm() || undefined, category: category || undefined } });
    this.loadProducts();
  }

  goToPage(page: number): void {
    this.pageNumber.set(page);
    this.loadProducts();
  }

  nextPage(): void {
    if (this.pageNumber() < this.totalPages()) {
      this.pageNumber.update(p => p + 1);
      this.loadProducts();
    }
  }

  previousPage(): void {
    if (this.pageNumber() > 1) {
      this.pageNumber.update(p => p - 1);
      this.loadProducts();
    }
  }

  getPages(): number[] {
    const total = this.totalPages();
    const current = this.pageNumber();
    const pages: number[] = [];
    const start = Math.max(1, current - 2);
    const end = Math.min(total, current + 2);
    for (let i = start; i <= end; i++) pages.push(i);
    return pages;
  }

  getFullImageUrl(path: string): string {
    return `http://localhost:5068${path}`;
  }

  addToCart(productId: number): void {
    if (!this.authService.isAuthenticated()) {
      this.router.navigate(['/login'], { queryParams: { returnUrl: '/products' } });
      return;
    }
    this.cartService.addItem({ productId, quantity: 1 }).subscribe({
      next: () => this.notification.showSuccess('Added to cart'),
      error: (err) => this.notification.showError(err.error?.error || 'Failed to add to cart')
    });
  }

  toggleWishlist(productId: number, event: Event): void {
    event.stopPropagation();
    if (!this.authService.isAuthenticated()) {
      this.router.navigate(['/login'], { queryParams: { returnUrl: '/products' } });
      return;
    }
    this.wishlistService.toggle(productId).subscribe({
      next: (res) => this.notification.showSuccess(res.message),
      error: (err) => this.notification.showError(err.error?.error || 'Failed to update wishlist')
    });
  }

  clearFilters(): void {
    this.categoryFilter.set('');
    this.searchTerm.set('');
    this.minPrice = null;
    this.maxPrice = null;
    this.pageNumber.set(1);
    this.loadProducts();
  }

  applyPriceFilter(): void {
    this.pageNumber.set(1);
    this.loadProducts();
  }
}