import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { CartService } from '../../services/cart.service';
import { OrderService } from '../../services/order.service';
import { NotificationService } from '../../services/notification.service';
import { Cart } from '../../models/cart.model';
import { CreateOrder } from '../../models/order.model';

@Component({
  selector: 'app-checkout',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './checkout.component.html',
  styleUrl: './checkout.component.scss'
})
export class CheckoutComponent implements OnInit {
  private readonly cartService = inject(CartService);
  private readonly orderService = inject(OrderService);
  readonly notification = inject(NotificationService);
  private readonly router = inject(Router);
  protected readonly notifications = this.notification.notifications;

  cart = signal<Cart | null>(null);
  loading = signal(true);
  placing = signal(false);

  orderForm: CreateOrder = {
    shippingName: '',
    shippingAddress: '',
    shippingCity: '',
    shippingZip: ''
  };

  ngOnInit(): void {
    this.loadCart();
  }

  loadCart(): void {
    this.loading.set(true);
    this.cartService.getCart().subscribe({
      next: (cart) => {
        this.cart.set(cart);
        this.loading.set(false);
        if (!cart.items || cart.items.length === 0) {
          this.notification.showError('Your cart is empty. Add items before checkout.');
          this.router.navigate(['/products']);
        }
      },
      error: (err) => {
        console.error('Failed to load cart:', err);
        this.notification.showError('Failed to load cart.');
        this.loading.set(false);
      }
    });
  }

  placeOrder(): void {
    const c = this.cart();
    if (!c || c.items.length === 0) return;

    this.placing.set(true);
    this.orderService.createOrder(this.orderForm).subscribe({
      next: (order) => {
        this.notification.showSuccess('Order placed successfully!');
        this.router.navigate(['/orders', order.id]);
      },
      error: (err) => {
        const msg = err.error?.error || 'Failed to place order.';
        this.notification.showError(msg);
        this.placing.set(false);
      }
    });
  }

  getFullImageUrl(path: string): string {
    return `http://localhost:5068${path}`;
  }
}
