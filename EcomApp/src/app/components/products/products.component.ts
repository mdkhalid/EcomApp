import { Component, inject, OnInit, OnDestroy, signal, computed, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink, withComponentInputBinding } from '@angular/router';
import { ProductService } from '../../services/product.service';
import { NotificationService } from '../../services/notification.service';
import { CartService } from '../../services/cart.service';
import { AuthService } from '../../services/auth.service';
import { WishlistService } from '../../services/wishlist.service';
import { CategoryService } from '../../services/category.service';
import { BannerService } from '../../services/banner.service';
import { ActivityService } from '../../services/activity.service';
import { Product, SearchFilter, SearchResult, FilterMetadata } from '../../models/product.model';
import { Category } from '../../models/category.model';
import { Banner } from '../../models/banner.model';
import { FilterSidebarComponent, FilterState } from '../filter-sidebar/filter-sidebar.component';
import { RecentlyViewedProduct } from '../../models/activity.model';
import { getFullImageUrl as buildImageUrl } from '../../utils/api-config';

@Component({
  selector: 'app-products',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, FilterSidebarComponent],
  templateUrl: './products.component.html',
  styleUrl: './products.component.scss'
})
export class ProductsComponent implements OnInit, OnDestroy {
  private readonly destroyRef = inject(DestroyRef);
  private readonly productService = inject(ProductService);
  private readonly cartService = inject(CartService);
  private readonly categoryService = inject(CategoryService);
  private readonly bannerService = inject(BannerService);
  readonly authService = inject(AuthService);
  readonly wishlistService = inject(WishlistService);
  readonly notification = inject(NotificationService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly activityService = inject(ActivityService);
  protected readonly Math = Math;
  protected readonly notifications = this.notification.notifications;

  products = signal<Product[]>([]);
  totalCount = signal(0);
  pageNumber = signal(1);
  pageSize = signal(12);
  searchTerm = signal('');
  categoryFilter = signal('');
  sortBy = signal('popularity');
  isLoading = signal(false);
  showMobileFilters = signal(false);
  minPrice: number | null = null;
  maxPrice: number | null = null;

  recentlyViewed = signal<RecentlyViewedProduct[]>([]);
  forYou = signal<RecentlyViewedProduct[]>([]);
  trending = signal<RecentlyViewedProduct[]>([]);

  categories = signal<Category[]>([]);
  banners = signal<Banner[]>([]);
  currentBanner = signal(0);
  private bannerInterval: ReturnType<typeof setInterval> | null = null;
  bannerPaused = signal(false);

  filters = signal<FilterMetadata | null>(null);
  currentFilter = signal<SearchFilter | null>(null);

  totalPages = computed(() => Math.ceil(this.totalCount() / this.pageSize()));

  ngOnInit(): void {
    if (this.authService.isAdmin()) {
      this.router.navigate(['/admin']);
      return;
    }
    this.categoryService.getAll().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(data => this.categories.set(data));
    this.bannerService.getActive().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(data => {
      this.banners.set(data);
      if (data.length > 0) this.startBannerRotation();
    });
    if (this.authService.isAuthenticated() && !this.authService.isAdmin()) {
      this.activityService.getRecentlyViewed().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: (items) => this.recentlyViewed.set(items)
      });
      this.activityService.getForYou().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: (items) => this.forYou.set(items)
      });
    }
    this.activityService.getTrending().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (items) => this.trending.set(items)
    });
    this.route.queryParams.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(params => {
      if (params['search']) {
        this.searchTerm.set(params['search']);
      }
      if (params['category']) {
        this.categoryFilter.set(params['category']);
      }
      if (params['brand']) {
        // Handle brand from URL
      }
      if (params['minPrice']) {
        this.minPrice = +params['minPrice'];
      }
      if (params['maxPrice']) {
        this.maxPrice = +params['maxPrice'];
      }
      if (params['minRating']) {
        // Handle rating from URL
      }
      if (params['minDiscount']) {
        // Handle discount from URL
      }
      if (params['inStock']) {
        // Handle inStock from URL
      }
      if (params['sortBy']) {
        this.sortBy.set(params['sortBy']);
      }
      if (params['page']) {
        this.pageNumber.set(+params['page']);
      }
      this.loadProducts();
    });
    this.startBannerRotation();
  }

  private imageCycles = new Map<number, { index: number; intervalId: ReturnType<typeof setInterval> }>();

  getProductImages(product: Product): string[] {
    return product.images?.map(i => i.imageUrl).filter(u => !!u) ?? [];
  }

  getCurrentImageIndex(productId: number): number {
    return this.imageCycles.get(productId)?.index ?? 0;
  }

  startImageCycle(product: Product): void {
    const urls = this.getProductImages(product);
    if (urls.length <= 1) return;
    if (this.imageCycles.has(product.id)) return;

    this.imageCycles.set(product.id, { index: 1 % urls.length, intervalId: null as unknown as ReturnType<typeof setInterval> });
    const cycle = this.imageCycles.get(product.id)!;
    cycle.intervalId = setInterval(() => {
      const c = this.imageCycles.get(product.id);
      if (!c) return;
      c.index = (c.index + 1) % urls.length;
    }, 1000);
  }

  stopImageCycle(product: Product): void {
    const cycle = this.imageCycles.get(product.id);
    if (cycle) {
      clearInterval(cycle.intervalId);
      this.imageCycles.delete(product.id);
    }
  }

  onImageDotClick(event: Event, product: Product, index: number): void {
    event.preventDefault();
    event.stopPropagation();
    this.stopImageCycle(product);
    this.imageCycles.set(product.id, { index, intervalId: null as unknown as ReturnType<typeof setInterval> });
  }

  ngOnDestroy(): void {
    this.stopBannerRotation();
    for (const cycle of this.imageCycles.values()) {
      clearInterval(cycle.intervalId);
    }
    this.imageCycles.clear();
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

    const filter: SearchFilter = {
      search: this.searchTerm() || undefined,
      category: this.categoryFilter() || undefined,
      minPrice: this.minPrice ?? undefined,
      maxPrice: this.maxPrice ?? undefined,
      sortBy: this.sortBy() || undefined,
      pageNumber: this.pageNumber(),
      pageSize: this.pageSize()
    };

    this.currentFilter.set(filter);

    this.productService.search(filter).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data: SearchResult<Product>) => {
        this.products.set(data.items);
        this.totalCount.set(data.totalCount);
        this.filters.set(data.filters);
        this.isLoading.set(false);
      },
      error: () => {
        this.notification.showError('Failed to load products.');
        this.isLoading.set(false);
      }
    });
  }

  onFilterChanged(filterState: FilterState): void {
    this.categoryFilter.set(filterState.categories.join(','));
    this.minPrice = filterState.minPrice;
    this.maxPrice = filterState.maxPrice;
    this.sortBy.set(filterState.sortBy);
    this.pageNumber.set(1);
    this.updateUrl();
    this.loadProducts();
  }

  onClearFilters(): void {
    this.searchTerm.set('');
    this.categoryFilter.set('');
    this.minPrice = null;
    this.maxPrice = null;
    this.sortBy.set('popularity');
    this.pageNumber.set(1);
    this.updateUrl();
    this.loadProducts();
  }

  private updateUrl(): void {
    const queryParams: any = {};
    if (this.searchTerm()) queryParams.search = this.searchTerm();
    if (this.categoryFilter()) queryParams.category = this.categoryFilter();
    if (this.minPrice != null) queryParams.minPrice = this.minPrice;
    if (this.maxPrice != null) queryParams.maxPrice = this.maxPrice;
    if (this.sortBy() !== 'popularity') queryParams.sortBy = this.sortBy();
    if (this.pageNumber() > 1) queryParams.page = this.pageNumber();

    this.router.navigate(['/products'], { queryParams, queryParamsHandling: 'merge' });
  }

  goToPage(page: number): void {
    this.pageNumber.set(page);
    this.updateUrl();
    this.loadProducts();
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  nextPage(): void {
    if (this.pageNumber() < this.totalPages()) {
      this.pageNumber.update(p => p + 1);
      this.updateUrl();
      this.loadProducts();
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }
  }

  previousPage(): void {
    if (this.pageNumber() > 1) {
      this.pageNumber.update(p => p - 1);
      this.updateUrl();
      this.loadProducts();
      window.scrollTo({ top: 0, behavior: 'smooth' });
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
    return buildImageUrl(path);
  }

  addToCart(productId: number): void {
    if (!this.authService.isAuthenticated()) {
      this.router.navigate(['/login'], { queryParams: { returnUrl: '/products' } });
      return;
    }
    this.cartService.addItem({ productId, quantity: 1 }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
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
    this.wishlistService.toggle(productId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (res) => this.notification.showSuccess(res.message),
      error: (err) => this.notification.showError(err.error?.error || 'Failed to update wishlist')
    });
  }

  clearFilters(): void {
    this.onClearFilters();
  }

  toggleMobileFilters(): void {
    this.showMobileFilters.update(v => !v);
  }
}
