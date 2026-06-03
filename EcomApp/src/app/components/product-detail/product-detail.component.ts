import { Component, inject, OnInit, signal, computed } from '@angular/core';
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
import { Product } from '../../models/product.model';
import { Review, CreateReview } from '../../models/review.model';
import { RecentlyViewedProduct } from '../../models/activity.model';

@Component({
  selector: 'app-product-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './product-detail.component.html',
  styleUrl: './product-detail.component.scss'
})
export class ProductDetailComponent implements OnInit {
  private readonly productService = inject(ProductService);
  private readonly reviewService = inject(ReviewService);
  private readonly cartService = inject(CartService);
  readonly wishlistService = inject(WishlistService);
  readonly authService = inject(AuthService);
  readonly notification = inject(NotificationService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly activityService = inject(ActivityService);
  protected readonly notifications = this.notification.notifications;
  protected readonly Math = Math;

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
    const images: { url: string; id: number }[] = p.images.map(i => ({ url: i.imageUrl, id: i.id }));
    if (p.imageUrl && !images.some(i => i.url === p.imageUrl)) {
      images.unshift({ url: p.imageUrl, id: 0 });
    }
    return images;
  }
  get currentImage(): string {
    const imgs = this.images;
    if (imgs.length === 0) return '';
    return this.getFullImageUrl(imgs[this.selectedImageIndex()].url);
  }

  recommended = signal<RecentlyViewedProduct[]>([]);

  newReview = signal<CreateReview>({ rating: 5, comment: '' });
  submittingReview = signal(false);
  hasReviewed = signal(false);
  hasPurchased = signal(false);
  reviewCheckMessage = signal('');

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (id) {
      this.loadProduct(id);
      this.loadReviews(id);
    }
    if (this.authService.isAuthenticated() && !this.authService.isAdmin()) {
      this.activityService.getForYou().subscribe({
        next: (items) => this.recommended.set(items)
      });
    }
  }

  loadProduct(id: number): void {
    this.productService.getById(id).subscribe({
      next: (product) => {
        this.product.set(product);
        this.avgRating.set(product.averageRating);
        this.reviewCount.set(product.totalReviews);
        this.loading.set(false);
        if (this.authService.isAuthenticated() && !this.authService.isAdmin()) {
          this.wishlistService.check(id).subscribe();
          this.checkCanReview(id);
        }
        this.activityService.logActivity('ProductView', String(id)).subscribe();
      },
      error: () => {
        this.notification.showError('Failed to load product');
        this.loading.set(false);
      }
    });
  }

  checkCanReview(productId: number): void {
    this.reviewService.canReview(productId).subscribe({
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
    this.reviewService.getByProduct(productId).subscribe({
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
    this.cartService.addItem({ productId: p.id, quantity: this.quantity() }).subscribe({
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
    this.wishlistService.toggle(p.id).subscribe({
      next: (res) => this.notification.showSuccess(res.message),
      error: (err) => this.notification.showError(err.error?.error || 'Failed to update wishlist')
    });
  }

  submitReview(): void {
    const p = this.product();
    if (!p) return;
    this.submittingReview.set(true);
    this.reviewService.create(p.id, this.newReview()).subscribe({
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

  setRating(n: number): void {
    this.newReview.update(r => ({ ...r, rating: n }));
  }

  getFullImageUrl(path: string): string {
    return `http://localhost:5068${path}`;
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
