import { Component, signal, inject, OnInit } from '@angular/core';
import { RouterLink, RouterOutlet, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService } from './services/auth.service';
import { CartService } from './services/cart.service';

@Component({
  selector: 'app-root',
  imports: [RouterLink, RouterOutlet, FormsModule],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App implements OnInit {
  protected readonly title = signal('ShopKart');
  protected readonly authService = inject(AuthService);
  protected readonly cartService = inject(CartService);
  protected readonly router = inject(Router);
  protected searchQuery = signal('');

  ngOnInit(): void {
    // Fetch cart count for non-admin users (admin carts throw 401 on backend)
    if (!this.authService.isAdmin()) {
      this.cartService.getCart().subscribe();
    }
  }

  protected onSearch(): void {
    const q = this.searchQuery().trim();
    if (q) {
      this.router.navigate(['/products'], { queryParams: { search: q } });
    }
  }

  protected logout(): void {
    this.cartService.resetCount();
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
