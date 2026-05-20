import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ProductService, PaginatedResponse } from '../../services/product.service';
import { NotificationService } from '../../services/notification.service';
import { CartService } from '../../services/cart.service';
import { AuthService } from '../../services/auth.service';
import { Product } from '../../models/product.model';

@Component({
  selector: 'app-products',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './products.component.html',
  styleUrl: './products.component.scss'
})
export class ProductsComponent implements OnInit {
  private readonly productService = inject(ProductService);
  private readonly cartService = inject(CartService);
  private readonly authService = inject(AuthService);
  readonly notification = inject(NotificationService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  protected readonly Math = Math;
  protected readonly notifications = this.notification.notifications;

  products = signal<Product[]>([]);
  totalCount = signal(0);
  pageNumber = signal(1);
  pageSize = signal(12);
  searchTerm = signal('');
  categoryFilter = signal('');
  sortBy = signal('');

  categories = ['Electronics', 'Clothing', 'Footwear', 'Home', 'Accessories'];

  totalPages = computed(() => Math.ceil(this.totalCount() / this.pageSize()));

  ngOnInit(): void {
    if (this.authService.isAdmin()) {
      this.router.navigate(['/admin']);
      return;
    }
    this.route.queryParams.subscribe(params => {
      if (params['search']) {
        this.searchTerm.set(params['search']);
      }
      this.loadProducts();
    });
  }

  loadProducts(): void {
    this.productService.getAll({
      pageNumber: this.pageNumber(),
      pageSize: this.pageSize(),
      search: this.searchTerm() || undefined,
      category: this.categoryFilter() || undefined
    }).subscribe({
      next: (data: PaginatedResponse<Product>) => {
        this.products.set(data.items);
        this.totalCount.set(data.totalCount);
      },
      error: () => this.notification.showError('Failed to load products.')
    });
  }

  onSearchInput(value: string): void {
    this.searchTerm.set(value);
  }

  onSearch(): void {
    this.pageNumber.set(1);
    this.router.navigate(['/products'], { queryParams: { search: this.searchTerm() || undefined, category: this.categoryFilter() || undefined } });
    this.loadProducts();
  }

  onCategoryChange(category: string): void {
    this.categoryFilter.set(category);
    this.pageNumber.set(1);
    this.router.navigate(['/products'], { queryParams: { search: this.searchTerm() || undefined, category: category || undefined } });
    this.loadProducts();
  }

  goToPage(page: number): void {
    this.pageNumber.set(page);
    this.loadProducts();
  }

  nextPage(): void {
    if (this.pageNumber() < this.totalPages()) {
      this.pageNumber.update(p => p + 1);
      this.loadProducts();
    }
  }

  previousPage(): void {
    if (this.pageNumber() > 1) {
      this.pageNumber.update(p => p - 1);
      this.loadProducts();
    }
  }

  getPages(): number[] {
    const total = this.totalPages();
    const current = this.pageNumber();
    const pages: number[] = [];
    const start = Math.max(1, current - 2);
    const end = Math.min(total, current + 2);
    for (let i = start; i <= end; i++) pages.push(i);
    return pages;
  }

  getFullImageUrl(path: string): string {
    return `http://localhost:5068${path}`;
  }

  addToCart(productId: number): void {
    if (!this.authService.isAuthenticated()) {
      this.router.navigate(['/login'], { queryParams: { returnUrl: '/products' } });
      return;
    }
    this.cartService.addItem({ productId, quantity: 1 }).subscribe({
      next: () => this.notification.showSuccess('Added to cart'),
      error: (err) => this.notification.showError(err.error?.error || 'Failed to add to cart')
    });
  }

  isLowStock(stock: number): boolean {
    return stock < 10;
  }
}
