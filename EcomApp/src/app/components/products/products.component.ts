import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ProductService, PaginatedResponse } from '../../services/product.service';
import { NotificationService } from '../../services/notification.service';
import { CartService } from '../../services/cart.service';
import { Product, CreateProduct } from '../../models/product.model';

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
  readonly notification = inject(NotificationService);
  protected readonly Math = Math;
  protected readonly notifications = this.notification.notifications;

  products = signal<Product[]>([]);
  totalCount = signal(0);
  pageNumber = signal(1);
  pageSize = signal(5);
  searchTerm = signal('');
  categoryFilter = signal('');
  showModal = false;
  editingProduct: Product | null = null;
  selectedFile: File | null = null;
  imagePreview: string | null = null;

  categories = ['Electronics', 'Clothing', 'Footwear', 'Home', 'Accessories'];

  totalPages = computed(() => Math.ceil(this.totalCount() / this.pageSize()));
  hasNextPage = computed(() => this.pageNumber() < this.totalPages());
  hasPreviousPage = computed(() => this.pageNumber() > 1);

  newProduct: CreateProduct = {
    name: '',
    description: '',
    price: 0,
    stock: 0,
    category: ''
  };

  ngOnInit(): void {
    this.loadProducts();
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
      error: (err) => this.notification.showError('Failed to load products.')
    });
  }

  onSearch(term: string): void {
    this.searchTerm.set(term);
    this.pageNumber.set(1);
    this.loadProducts();
  }

  onCategoryChange(category: string): void {
    this.categoryFilter.set(category);
    this.pageNumber.set(1);
    this.loadProducts();
  }

  goToPage(page: number): void {
    this.pageNumber.set(page);
    this.loadProducts();
  }

  nextPage(): void {
    if (this.hasNextPage()) {
      this.pageNumber.update(p => p + 1);
      this.loadProducts();
    }
  }

  previousPage(): void {
    if (this.hasPreviousPage()) {
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

    for (let i = start; i <= end; i++) {
      pages.push(i);
    }
    return pages;
  }

  openAddModal(): void {
    this.editingProduct = null;
    this.newProduct = { name: '', description: '', price: 0, stock: 0, category: '' };
    this.selectedFile = null;
    this.imagePreview = null;
    this.showModal = true;
  }

  openEditModal(product: Product): void {
    this.editingProduct = product;
    this.newProduct = { ...product };
    this.selectedFile = null;
    this.imagePreview = product.imageUrl ? this.getFullImageUrl(product.imageUrl) : null;
    this.showModal = true;
  }

  closeModal(): void {
    this.showModal = false;
    this.editingProduct = null;
    this.selectedFile = null;
    this.imagePreview = null;
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.selectedFile = input.files[0];
      const reader = new FileReader();
      reader.onload = () => this.imagePreview = reader.result as string;
      reader.readAsDataURL(this.selectedFile);
    }
  }

  uploadImage(): void {
    if (!this.editingProduct || !this.selectedFile) return;
    this.productService.uploadImage(this.editingProduct.id, this.selectedFile).subscribe({
      next: () => {
        this.loadProducts();
        this.notification.showSuccess('Image uploaded successfully.');
      },
      error: (err) => {
        const msg = err.error?.error || 'Failed to upload image.';
        this.notification.showError(msg);
      }
    });
  }

  getFullImageUrl(path: string): string {
    return `http://localhost:5068${path}`;
  }

  saveProduct(): void {
    if (this.editingProduct) {
      this.productService.update(this.editingProduct.id, this.newProduct).subscribe({
        next: (updated) => {
          if (this.selectedFile) {
            this.uploadImageAndClose(updated.id);
          } else {
            this.loadProducts();
            this.closeModal();
            this.notification.showSuccess('Product updated successfully.');
          }
        },
        error: (err) => {
          const msg = err.error?.error || 'Failed to update product.';
          this.notification.showError(msg);
        }
      });
    } else {
      this.productService.create(this.newProduct).subscribe({
        next: (created) => {
          if (this.selectedFile) {
            this.uploadImageAndClose(created.id);
          } else {
            this.loadProducts();
            this.closeModal();
            this.notification.showSuccess('Product created successfully.');
          }
        },
        error: (err) => {
          const msg = err.error?.error || 'Failed to create product.';
          this.notification.showError(msg);
        }
      });
    }
  }

  private uploadImageAndClose(productId: number): void {
    this.productService.uploadImage(productId, this.selectedFile!).subscribe({
      next: () => {
        this.loadProducts();
        this.closeModal();
        this.notification.showSuccess('Product saved with image.');
      },
      error: (err) => {
        this.loadProducts();
        this.closeModal();
        const msg = err.error?.error || 'Product saved but image upload failed.';
        this.notification.showError(msg);
      }
    });
  }

  addToCart(productId: number): void {
    this.cartService.addItem({ productId, quantity: 1 }).subscribe({
      next: () => this.notification.showSuccess('Item added to cart.'),
      error: (err) => {
        const msg = err.error?.error || 'Failed to add item to cart.';
        this.notification.showError(msg);
      }
    });
  }

  deleteProduct(id: number): void {
    if (confirm('Are you sure you want to delete this product?')) {
      this.productService.delete(id).subscribe({
        next: () => {
          this.loadProducts();
          this.notification.showSuccess('Product deleted successfully.');
        },
        error: (err) => {
          const msg = err.error?.error || 'Failed to delete product.';
          this.notification.showError(msg);
        }
      });
    }
  }
}