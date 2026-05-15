import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ProductService, PaginatedResponse } from '../../services/product.service';
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
  protected readonly Math = Math;

  products = signal<Product[]>([]);
  totalCount = signal(0);
  pageNumber = signal(1);
  pageSize = signal(5);
  showModal = false;
  editingProduct: Product | null = null;

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
    this.productService.getAll({ pageNumber: this.pageNumber(), pageSize: this.pageSize() }).subscribe({
      next: (data: PaginatedResponse<Product>) => {
        console.log('Data received:', data);
        this.products.set(data.items);
        this.totalCount.set(data.totalCount);
      },
      error: (err) => console.error('Error loading products', err)
    });
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
    this.showModal = true;
  }

  openEditModal(product: Product): void {
    this.editingProduct = product;
    this.newProduct = { ...product };
    this.showModal = true;
  }

  closeModal(): void {
    this.showModal = false;
    this.editingProduct = null;
  }

  saveProduct(): void {
    if (this.editingProduct) {
      this.productService.update(this.editingProduct.id, this.newProduct).subscribe({
        next: () => {
          this.loadProducts();
          this.closeModal();
        },
        error: (err) => console.error('Error updating product', err)
      });
    } else {
      this.productService.create(this.newProduct).subscribe({
        next: () => {
          this.loadProducts();
          this.closeModal();
        },
        error: (err) => console.error('Error creating product', err)
      });
    }
  }

  deleteProduct(id: number): void {
    if (confirm('Are you sure you want to delete this product?')) {
      this.productService.delete(id).subscribe({
        next: () => this.loadProducts(),
        error: (err) => console.error('Error deleting product', err)
      });
    }
  }
}