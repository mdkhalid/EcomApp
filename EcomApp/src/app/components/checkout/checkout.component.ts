import { Component, inject, OnInit, signal, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, NgForm } from '@angular/forms';
import { Router } from '@angular/router';
import { CartService } from '../../services/cart.service';
import { OrderService } from '../../services/order.service';
import { NotificationService } from '../../services/notification.service';
import { AuthService } from '../../services/auth.service';
import { Cart } from '../../models/cart.model';
import { CreateOrder, SavedAddress } from '../../models/order.model';
import { Address, CreateAddressRequest } from '../../models/auth.model';

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
  private readonly authService = inject(AuthService);
  protected readonly notifications = this.notification.notifications;

  @ViewChild('checkoutForm') checkoutForm!: NgForm;

  cart = signal<Cart | null>(null);
  loading = signal(true);
  placing = signal(false);
  savedAddresses = signal<SavedAddress[]>([]);
  selectedSavedAddress = signal<number | null>(null);

  // Address Book
  addressBookAddresses = signal<Address[]>([]);
  selectedAddressBook = signal<number | null>(null);
  showNewAddressForm = signal(false);
  newAddressSaving = signal(false);
  newAddress = signal({
    name: '',
    label: 'Home',
    street: '',
    city: '',
    state: '',
    zipCode: '',
    country: '',
    isDefault: false
  });

  orderForm: CreateOrder = {
    shippingName: '',
    shippingAddress: '',
    shippingCity: '',
    shippingZip: ''
  };

  ngOnInit(): void {
    if (!this.authService.isAuthenticated()) {
      this.router.navigate(['/login'], { queryParams: { returnUrl: '/checkout' } });
      return;
    }
    this.loadCart();
    this.loadAddressBook();
    this.loadSavedAddresses();
  }

  loadAddressBook(): void {
    this.authService.getAddresses().subscribe({
      next: (addresses) => this.addressBookAddresses.set(addresses),
      error: () => {}
    });
  }

  loadSavedAddresses(): void {
    this.orderService.getPreviousAddresses().subscribe({
      next: (addresses) => this.savedAddresses.set(addresses),
      error: () => {}
    });
  }

  selectAddressBookAddress(address: Address): void {
    if (this.selectedAddressBook() === address.id) {
      this.selectedAddressBook.set(null);
      this.resetOrderForm();
      return;
    }
    this.selectedAddressBook.set(address.id);
    this.selectedSavedAddress.set(null);
    const user = this.authService.currentUser();
    this.orderForm = {
      shippingName: user ? `${user.firstName || ''} ${user.lastName || ''}`.trim() || user.username : '',
      shippingAddress: address.street,
      shippingCity: address.city,
      shippingZip: address.zipCode
    };
    this.syncFormState();
  }

  selectPreviousAddress(index: number): void {
    if (this.selectedSavedAddress() === index) {
      this.selectedSavedAddress.set(null);
      this.resetOrderForm();
      return;
    }
    this.selectedSavedAddress.set(null);
    this.selectedAddressBook.set(null);
    this.selectedSavedAddress.set(index);
    const addr = this.savedAddresses()[index];
    this.orderForm = {
      shippingName: addr.name,
      shippingAddress: addr.address,
      shippingCity: addr.city,
      shippingZip: addr.zip
    };
    this.syncFormState();
  }

  clearForm(): void {
    this.selectedSavedAddress.set(null);
    this.selectedAddressBook.set(null);
    this.resetOrderForm();
  }

  private resetOrderForm(): void {
    this.orderForm = { shippingName: '', shippingAddress: '', shippingCity: '', shippingZip: '' };
    this.syncFormState();
  }

  private syncFormState(): void {
    setTimeout(() => {
      if (this.checkoutForm) {
        Object.keys(this.checkoutForm.controls).forEach(key => {
          const control = this.checkoutForm.controls[key];
          control.markAsDirty();
          control.markAsTouched();
          control.updateValueAndValidity();
        });
      }
    });
  }

  // Inline new address
  toggleNewAddressForm(): void {
    this.showNewAddressForm.set(!this.showNewAddressForm());
    if (!this.showNewAddressForm()) {
      this.newAddress.set({ name: '', label: 'Home', street: '', city: '', state: '', zipCode: '', country: '', isDefault: false });
    }
  }

  saveNewAddress(): void {
    const addr = this.newAddress();
    if (!addr.name || !addr.street || !addr.city || !addr.state || !addr.zipCode || !addr.country) {
      this.notification.showError('Please fill in all fields including name');
      return;
    }
    this.newAddressSaving.set(true);
    const { name, ...addressData } = addr;
    this.authService.addAddress(addressData as CreateAddressRequest).subscribe({
      next: (saved) => {
        this.newAddressSaving.set(false);
        this.showNewAddressForm.set(false);
        this.newAddress.set({ name: '', label: 'Home', street: '', city: '', state: '', zipCode: '', country: '', isDefault: false });
        this.loadAddressBook();
        // Auto-select the newly added address with the name
        this.selectedAddressBook.set(saved.id);
        this.selectedSavedAddress.set(null);
        this.orderForm = {
          shippingName: addr.name,
          shippingAddress: saved.street,
          shippingCity: saved.city,
          shippingZip: saved.zipCode
        };
        this.syncFormState();
        this.notification.showSuccess('Address saved and selected');
      },
      error: () => {
        this.newAddressSaving.set(false);
        this.notification.showError('Failed to save address');
      }
    });
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
        this.cartService.resetCount();
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
