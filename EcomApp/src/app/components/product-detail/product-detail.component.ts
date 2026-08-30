import { Component, inject, OnInit, signal, computed, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ProductService } from '../../services/product.service';
import { ReviewService } from '../../services/review.service';
import { CartService } from '../../services/cart.service';
import { WishlistService } from '../../services/wishlist.service';
import { AuthService } from '../../services/auth.service';
import { NotificationService } from '../../services/notification.service';
import { ActivityService } from '../../services/activity.service';
import { Product, ProductVariant } from '../../models/product.model';
import { Review, CreateReview } from '../../models/review.model';
import { RecentlyViewedProduct } from '../../models/activity.model';
import { StockAlertService, CreateStockAlertDto } from '../../services/stock-alert.service';
import { getFullImageUrl as buildImageUrl } from '../../utils/api-config';

@Component({
  selector: 'app-product-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './product-detail.component.html',
  styleUrl: './product-detail.component.scss'
})
export class ProductDetailComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly productService = inject(ProductService);
  private readonly reviewService = inject(ReviewService);
  private readonly cartService = inject(CartService);
  readonly wishlistService = inject(WishlistService);
  readonly authService = inject(AuthService);
  readonly notification = inject(NotificationService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly activityService = inject(ActivityService);
  private readonly stockAlertService = inject(StockAlertService);
  protected readonly notifications = this.notification.notifications;
  protected readonly Math = Math;

  subscribingToAlert = signal(false);

  product = signal<Product | null>(null);
  reviews = signal<Review[]>([]);
  reviewCount = signal(0);
  avgRating = signal(0);
  loading = signal(true);
  quantity = signal(1);

  selectedImageIndex = signal(0);
  get images(): { url: string; id: number }[] {
    const p = this.product();
    if (!p) return [];
    return p.images.map(i => ({ url: i.imageUrl, id: i.id }));
  }
  get currentImage(): string {
    const imgs = this.images;
    if (imgs.length === 0) return '';
    return this.getFullImageUrl(imgs[this.selectedImageIndex()].url);
  }

  selectedVariant = signal<ProductVariant | null>(null);

  get effectivePrice(): number {
    return this.selectedVariant()?.price ?? this.product()?.price ?? 0;
  }
  get effectiveStock(): number {
    return this.selectedVariant()?.stock ?? this.product()?.stock ?? 0;
  }
  get hasVariants(): boolean {
    return (this.product()?.variants?.length ?? 0) > 0;
  }
  get effectiveOriginalPrice(): number | undefined {
    return this.product()?.originalPrice;
  }

  recommended = signal<RecentlyViewedProduct[]>([]);
  alsoBought = signal<RecentlyViewedProduct[]>([]);
  frequentlyBought = signal<RecentlyViewedProduct[]>([]);

  newReview = signal<CreateReview>({ rating: 5, comment: '' });
  submittingReview = signal(false);
  hasReviewed = signal(false);
  hasPurchased = signal(false);
  reviewCheckMessage = signal('');

  ngOnInit(): void {
    this.route.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(params => {
      const id = Number(params.get('id'));
      if (id) {
        this.loading.set(true);
        this.loadProduct(id);
        this.loadReviews(id);
        this.loadAlsoBought(id);
        this.loadFrequentlyBought(id);
      }
    });
    if (this.authService.isAuthenticated() && !this.authService.isAdmin()) {
      this.activityService.getForYou().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: (items) => this.recommended.set(items)
      });
    }
  }

  loadProduct(id: number): void {
    this.productService.getById(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (product) => {
        this.product.set(product);
        this.avgRating.set(product.averageRating);
        this.reviewCount.set(product.totalReviews);
        this.selectedVariant.set(product.variants?.length > 0 ? product.variants[0] : null);
        this.loading.set(false);
        if (this.authService.isAuthenticated() && !this.authService.isAdmin()) {
          this.wishlistService.check(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe();
          this.checkCanReview(id);
        }
        this.activityService.logActivity('ProductView', String(id)).pipe(takeUntilDestroyed(this.destroyRef)).subscribe();
      },
      error: () => {
        this.notification.showError('Failed to load product');
        this.loading.set(false);
      }
    });
  }

  checkCanReview(productId: number): void {
    this.reviewService.canReview(productId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (res) => {
        this.hasPurchased.set(res.canReview);
        this.reviewCheckMessage.set(res.reason);
      },
      error: () => {
        this.hasPurchased.set(false);
        this.reviewCheckMessage.set('Unable to verify purchase');
      }
    });
  }

  loadReviews(productId: number): void {
    this.reviewService.getByProduct(productId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (res) => {
        this.reviews.set(res.items);
        this.avgRating.set(res.averageRating);
        this.reviewCount.set(res.totalReviews);
      }
    });
  }

  addToCart(): void {
    const p = this.product();
    if (!p) return;
    if (!this.authService.isAuthenticated()) {
      this.router.navigate(['/login'], { queryParams: { returnUrl: `/products/${p.id}` } });
      return;
    }
    const variantId = this.selectedVariant()?.id;
    this.cartService.addItem({ productId: p.id, quantity: this.quantity(), productVariantId: variantId }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => this.notification.showSuccess('Added to cart'),
      error: (err) => this.notification.showError(err.error?.error || 'Failed to add to cart')
    });
  }

  addSuggestionToCart(productId: number): void {
    if (!this.authService.isAuthenticated()) {
      this.router.navigate(['/login'], { queryParams: { returnUrl: this.router.url } });
      return;
    }
    this.cartService.addItem({ productId, quantity: 1 }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => this.notification.showSuccess('Added to cart'),
      error: (err) => this.notification.showError(err.error?.error || 'Failed to add to cart')
    });
  }

  toggleWishlist(): void {
    const p = this.product();
    if (!p) return;
    if (!this.authService.isAuthenticated()) {
      this.router.navigate(['/login'], { queryParams: { returnUrl: `/products/${p.id}` } });
      return;
    }
    this.wishlistService.toggle(p.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (res) => this.notification.showSuccess(res.message),
      error: (err) => this.notification.showError(err.error?.error || 'Failed to update wishlist')
    });
  }

  submitReview(): void {
    const p = this.product();
    if (!p) return;
    this.submittingReview.set(true);
    this.reviewService.create(p.id, this.newReview()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (review) => {
        this.notification.showSuccess('Review submitted!');
        this.reviews.update(r => [review, ...r]);
        this.hasReviewed.set(true);
        this.newReview.set({ rating: 5, comment: '' });
        this.loadReviews(p.id);
        this.submittingReview.set(false);
      },
      error: (err) => {
        this.submittingReview.set(false);
        this.notification.showError(err.error?.error || 'Failed to submit review');
      }
    });
  }

  selectVariant(v: ProductVariant): void {
    this.selectedVariant.set(v);
    this.quantity.set(1);
  }

  setRating(n: number): void {
    this.newReview.update(r => ({ ...r, rating: n }));
  }

  getFullImageUrl(path: string): string {
    return buildImageUrl(path);
  }

  subscribeToStockAlert(): void {
    const p = this.product();
    if (!p) return;
    if (!this.authService.isAuthenticated()) {
      this.router.navigate(['/login'], { queryParams: { returnUrl: `/products/${p.id}` } });
      return;
    }

    const variantId = this.selectedVariant()?.id;
    const dto: CreateStockAlertDto = { productId: p.id, variantId: variantId ?? undefined };

    this.subscribingToAlert.set(true);
    this.stockAlertService.createAlert(dto).subscribe({
      next: () => {
        this.notification.showSuccess('You will be notified when this product is back in stock!');
        this.subscribingToAlert.set(false);
      },
      error: (err) => {
        this.notification.showError(err.error?.error || 'Failed to subscribe to stock alert');
        this.subscribingToAlert.set(false);
      }
    });
  }

  loadAlsoBought(productId: number): void {
    this.activityService.getAlsoBought(productId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (items) => this.alsoBought.set(items)
    });
  }

  loadFrequentlyBought(productId: number): void {
    this.activityService.getFrequentlyBoughtTogether(productId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (items) => this.frequentlyBought.set(items)
    });
  }

  getFormattedDate(dateStr: string): string {
    const d = new Date(dateStr);
    return d.toLocaleDateString('en-IN', { year: 'numeric', month: 'short', day: 'numeric' });
  }

  getRatingPercent(star: number): number {
    const total = this.reviewCount();
    if (total === 0) return 0;
    const count = this.reviews().filter(r => Math.round(r.rating) === star).length;
    return (count / total) * 100;
  }

  getRatingCount(star: number): number {
    return this.reviews().filter(r => Math.round(r.rating) === star).length;
  }
}
