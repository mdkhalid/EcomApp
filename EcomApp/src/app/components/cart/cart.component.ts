import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CartService } from '../../services/cart.service';
import { NotificationService } from '../../services/notification.service';
import { Cart, CartItem } from '../../models/cart.model';

@Component({
  selector: 'app-cart',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './cart.component.html',
  styleUrl: './cart.component.scss'
})
export class CartComponent implements OnInit {
  private readonly cartService = inject(CartService);
  readonly notification = inject(NotificationService);
  protected readonly notifications = this.notification.notifications;

  cart: Cart | null = null;
  loading = true;

  ngOnInit(): void {
    this.loadCart();
  }

  loadCart(): void {
    this.loading = true;
    this.cartService.getCart().subscribe({
      next: (cart) => {
        this.cart = cart;
        this.loading = false;
      },
      error: () => {
        this.notification.showError('Failed to load cart.');
        this.loading = false;
      }
    });
  }

  addItem(productId: number): void {
    this.cartService.addItem({ productId, quantity: 1 }).subscribe({
      next: (cart) => {
        this.cart = cart;
        this.notification.showSuccess('Item added to cart.');
      },
      error: (err) => {
        const msg = err.error?.error || 'Failed to add item.';
        this.notification.showError(msg);
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
      next: (cart) => this.cart = cart,
      error: (err) => {
        const msg = err.error?.error || 'Failed to update quantity.';
        this.notification.showError(msg);
      }
    });
  }

  removeItem(cartItemId: number): void {
    this.cartService.removeItem(cartItemId).subscribe({
      next: (cart) => {
        this.cart = cart;
        this.notification.showSuccess('Item removed from cart.');
      },
      error: () => this.notification.showError('Failed to remove item.')
    });
  }

  clearCart(): void {
    this.cartService.clearCart().subscribe({
      next: (cart) => {
        this.cart = cart;
        this.notification.showSuccess('Cart cleared.');
      },
      error: () => this.notification.showError('Failed to clear cart.')
    });
  }

  getFullImageUrl(path: string): string {
    return `http://localhost:5068${path}`;
  }
}
