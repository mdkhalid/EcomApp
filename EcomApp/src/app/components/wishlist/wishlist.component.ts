import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { WishlistService } from '../../services/wishlist.service';
import { CartService } from '../../services/cart.service';
import { NotificationService } from '../../services/notification.service';
import { WishlistItem } from '../../models/wishlist.model';
import { getFullImageUrl as buildImageUrl } from '../../utils/api-config';

@Component({
  selector: 'app-wishlist',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './wishlist.component.html',
  styleUrl: './wishlist.component.scss'
})
export class WishlistComponent implements OnInit {
  readonly wishlistService = inject(WishlistService);
  private readonly cartService = inject(CartService);
  readonly notification = inject(NotificationService);
  protected readonly notifications = this.notification.notifications;

  items = signal<WishlistItem[]>([]);
  loading = signal(true);

  ngOnInit(): void {
    this.loadWishlist();
  }

  loadWishlist(): void {
    this.loading.set(true);
    this.wishlistService.getWishlist().subscribe({
      next: (res) => {
        this.items.set(res.items);
        this.loading.set(false);
      },
      error: () => {
        this.notification.showError('Failed to load wishlist');
        this.loading.set(false);
      }
    });
  }

  removeItem(productId: number): void {
    this.wishlistService.toggle(productId).subscribe({
      next: (res) => {
        this.notification.showSuccess(res.message);
        this.items.update(items => items.filter(i => i.productId !== productId));
      },
      error: (err) => this.notification.showError(err.error?.error || 'Failed to remove')
    });
  }

  addToCart(productId: number): void {
    this.cartService.addItem({ productId, quantity: 1 }).subscribe({
      next: () => {
        this.notification.showSuccess('Added to cart');
        this.items.update(items => items.filter(i => i.productId !== productId));
      },
      error: (err) => this.notification.showError(err.error?.error || 'Failed to add to cart')
    });
  }

  getFullImageUrl(path: string): string {
    return `http://localhost:5068${path}`;
  }
}
