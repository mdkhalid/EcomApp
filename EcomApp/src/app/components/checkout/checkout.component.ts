import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { CartService } from '../../services/cart.service';
import { OrderService } from '../../services/order.service';
import { NotificationService } from '../../services/notification.service';
import { Cart } from '../../models/cart.model';
import { CreateOrder, SavedAddress } from '../../models/order.model';
import { AuthService } from '../../services/auth.service';

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

  private readonly authService = inject(AuthService);

  cart = signal<Cart | null>(null);
  loading = signal(true);
  placing = signal(false);
  savedAddresses = signal<SavedAddress[]>([]);
  selectedSavedAddress = signal<number | null>(null);

  orderForm: CreateOrder = {
    shippingName: '',
    shippingAddress: '',
    shippingCity: '',
    shippingZip: ''
  };

  ngOnInit(): void {
    this.loadCart();
    if (this.authService.isAuthenticated()) {
      this.loadSavedAddresses();
    }
  }

  loadSavedAddresses(): void {
    this.orderService.getPreviousAddresses().subscribe({
      next: (addresses) => this.savedAddresses.set(addresses),
      error: () => {}
    });
  }

  selectAddress(index: number): void {
    if (this.selectedSavedAddress() === index) {
      this.selectedSavedAddress.set(null);
      this.orderForm = { shippingName: '', shippingAddress: '', shippingCity: '', shippingZip: '' };
      return;
    }
    this.selectedSavedAddress.set(index);
    const addr = this.savedAddresses()[index];
    this.orderForm = {
      shippingName: addr.name,
      shippingAddress: addr.address,
      shippingCity: addr.city,
      shippingZip: addr.zip
    };
  }

  clearForm(): void {
    this.selectedSavedAddress.set(null);
    this.orderForm = { shippingName: '', shippingAddress: '', shippingCity: '', shippingZip: '' };
  }

  loadCart(): void {
    this.loading.set(true);
    this.cartService.getCart().subscribe({
      next: (cart) => {
        this.cart.set(cart);
        this.loading.set(false);
        if (!cart.items || cart.items.length === 0) {
          this.notification.showError('Your cart is empty');
          this.router.navigate(['/products']);
        }
      },
      error: () => {
        this.notification.showError('Failed to load cart');
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
        this.notification.showError(err.error?.error || 'Failed to place order');
        this.placing.set(false);
      }
    });
  }

  getFullImageUrl(path: string): string {
    return `http://localhost:5068${path}`;
  }
}
