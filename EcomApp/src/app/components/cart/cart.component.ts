import { Component, inject, OnInit, signal, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { CartService } from '../../services/cart.service';
import { CouponService } from '../../services/coupon.service';
import { NotificationService } from '../../services/notification.service';
import { ActivityService } from '../../services/activity.service';
import { Cart, CartItem } from '../../models/cart.model';
import { ValidateCouponResponse } from '../../models/coupon.model';
import { RecentlyViewedProduct } from '../../models/activity.model';
import { getFullImageUrl as buildImageUrl } from '../../utils/api-config';

@Component({
  selector: 'app-cart',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './cart.component.html',
  styleUrl: './cart.component.scss'
})
export class CartComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly cartService = inject(CartService);
  private readonly couponService = inject(CouponService);
  private readonly activityService = inject(ActivityService);
  private readonly router = inject(Router);
  readonly notification = inject(NotificationService);
  protected readonly notifications = this.notification.notifications;

  cart = signal<Cart | null>(null);
  loading = signal(true);
  suggestions = signal<RecentlyViewedProduct[]>([]);
  protected readonly Math = Math;

  couponCode = signal('');
  applyingCoupon = signal(false);
  couponResult = signal<ValidateCouponResponse | null>(null);
  couponError = signal('');
  showCouponInput = signal(false);

  ngOnInit(): void {
    this.loadCart();
    this.loadSuggestions();
  }

  loadSuggestions(): void {
    this.activityService.getTrending().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (items) => this.suggestions.set(items.slice(0, 6))
    });
  }

  loadCart(): void {
    this.loading.set(true);
    this.cartService.getCart().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (cart) => {
        this.cart.set(cart);
        this.loading.set(false);
      },
      error: () => {
        this.notification.showError('Failed to load cart');
        this.loading.set(false);
      }
    });
  }

  updateQuantity(item: CartItem, change: number): void {
    const newQty = item.quantity + change;
    if (newQty < 1) {
      this.removeItem(item.id);
      return;
    }
    this.cartService.updateItem(item.id, { quantity: newQty }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (cart) => this.cart.set(cart),
      error: (err) => this.notification.showError(err.error?.error || 'Failed to update quantity')
    });
  }

  removeItem(cartItemId: number): void {
    this.cartService.removeItem(cartItemId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (cart) => {
        this.cart.set(cart);
        this.notification.showSuccess('Item removed');
      },
      error: () => this.notification.showError('Failed to remove item')
    });
  }

  clearCart(): void {
    this.cartService.clearCart().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (cart) => {
        this.cart.set(cart);
        this.notification.showSuccess('Cart cleared');
      },
      error: () => this.notification.showError('Failed to clear cart')
    });
  }

  goToCheckout(): void {
    this.router.navigate(['/checkout']);
  }

  getItemCount(): number {
    const c = this.cart();
    return c ? c.items.reduce((sum, item) => sum + item.quantity, 0) : 0;
  }

  getFullImageUrl(path: string): string {
    return buildImageUrl(path);
  }

  addToCart(productId: number): void {
    this.cartService.addItem({ productId, quantity: 1 }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (cart) => {
        this.cart.set(cart);
        this.notification.showSuccess('Item added to cart');
      },
      error: () => this.notification.showError('Failed to add item')
    });
  }

  applyCoupon(): void {
    const code = this.couponCode().trim();
    if (!code) return;
    this.applyingCoupon.set(true);
    this.couponError.set('');
    this.couponResult.set(null);

    const cartTotal = this.cart()!.totalAmount;
    this.couponService.validate({ code, cartTotal }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result) => {
        this.applyingCoupon.set(false);
        if (result.isValid) {
          this.couponResult.set(result);
          this.couponError.set('');
        } else {
          this.couponError.set(result.errorMessage || 'Invalid coupon');
          this.couponResult.set(null);
        }
      },
      error: () => {
        this.applyingCoupon.set(false);
        this.couponError.set('Failed to validate coupon');
      }
    });
  }

  removeCoupon(): void {
    this.couponResult.set(null);
    this.couponCode.set('');
    this.couponError.set('');
  }

  toggleCouponInput(): void {
    this.showCouponInput.set(!this.showCouponInput());
    if (!this.showCouponInput()) {
      this.removeCoupon();
    }
  }

  getDiscountedTotal(): number {
    const result = this.couponResult();
    return result && result.isValid ? result.finalTotal : this.cart()!.totalAmount;
  }

  getDiscountAmount(): number {
    const result = this.couponResult();
    return result && result.isValid ? result.discountAmount : 0;
  }
}
