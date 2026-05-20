import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { CartService } from '../../services/cart.service';
import { NotificationService } from '../../services/notification.service';
import { Cart, CartItem } from '../../models/cart.model';

@Component({
  selector: 'app-cart',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './cart.component.html',
  styleUrl: './cart.component.scss'
})
export class CartComponent implements OnInit {
  private readonly cartService = inject(CartService);
  private readonly router = inject(Router);
  readonly notification = inject(NotificationService);
  protected readonly notifications = this.notification.notifications;

  cart = signal<Cart | null>(null);
  loading = signal(true);

  ngOnInit(): void {
    this.loadCart();
  }

  loadCart(): void {
    this.loading.set(true);
    this.cartService.getCart().subscribe({
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
    this.cartService.updateItem(item.id, { quantity: newQty }).subscribe({
      next: (cart) => this.cart.set(cart),
      error: (err) => this.notification.showError(err.error?.error || 'Failed to update quantity')
    });
  }

  removeItem(cartItemId: number): void {
    this.cartService.removeItem(cartItemId).subscribe({
      next: (cart) => {
        this.cart.set(cart);
        this.notification.showSuccess('Item removed');
      },
      error: () => this.notification.showError('Failed to remove item')
    });
  }

  clearCart(): void {
    this.cartService.clearCart().subscribe({
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
    return `http://localhost:5068${path}`;
  }
}
