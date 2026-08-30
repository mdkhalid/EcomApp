import { Component, inject, signal, OnInit, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ProductService } from '../../../services/product.service';
import { CategoryService } from '../../../services/category.service';
import { NotificationService } from '../../../services/notification.service';
import { Product, CreateProduct, UpdateProduct, ProductImage, ProductVariant, CreateProductVariant } from '../../../models/product.model';
import { Category } from '../../../models/category.model';
import { AdminTableComponent, ColumnDef, ActionDef } from '../shared/admin-table.component';
import { AdminModalComponent } from '../shared/admin-modal.component';

@Component({
  selector: 'app-admin-products',
  standalone: true,
  imports: [CommonModule, FormsModule, AdminTableComponent, AdminModalComponent],
  template: `
    <div class="admin-products">
      <header class="section-header">
        <h2>Products</h2>
        <button class="btn btn-primary" (click)="openAddProduct()">Add Product</button>
      </header>

      <div class="filters">
        <input type="text" [(ngModel)]="search" (ngModelChange)="onSearch()" placeholder="Search products..." class="search-input">
        <select [(ngModel)]="categoryFilter" (ngModelChange)="onSearch()" class="filter-select">
          <option value="">All Categories</option>
          @for (cat of categories(); track cat.id) {
            <option [value]="cat.id">{{ cat.name }}</option>
          }
        </select>
      </div>

      <app-admin-table
        [data]="filteredProducts()"
        [columns]="productColumns"
        [actions]="productActions"
        [pagination]="{ current: 1, total: 1 }"
        emptyMessage="No products found"
      />

      @if (showProductModal()) {
        <app-admin-modal
          [isOpen]="showProductModal()"
          [title]="editingProduct() ? 'Edit Product' : 'Add Product'"
          [loading]="saving()"
          (close)="closeProductModal()"
          (confirm)="saveProduct()">
          <form class="product-form">
            <div class="form-row">
              <div class="form-group">
                <label>Name *</label>
                <input type="text" [(ngModel)]="productForm.name" name="name" required>
              </div>
              <div class="form-group">
                <label>Category *</label>
                <select [(ngModel)]="productForm.category" name="category" required>
                  @for (cat of categories(); track cat.id) {
                    <option [value]="cat.id">{{ cat.name }}</option>
                  }
                </select>
              </div>
            </div>
            <div class="form-group">
              <label>Description</label>
              <textarea [(ngModel)]="productForm.description" name="description" rows="3"></textarea>
            </div>
            <div class="form-row">
              <div class="form-group">
                <label>Price *</label>
                <input type="number" [(ngModel)]="productForm.price" name="price" step="0.01" min="0" required>
              </div>
              <div class="form-group">
                <label>Original Price</label>
                <input type="number" [(ngModel)]="productForm.originalPrice" name="originalPrice" step="0.01" min="0">
              </div>
            </div>
            <div class="form-row">
              <div class="form-group">
                <label>Stock *</label>
                <input type="number" [(ngModel)]="productForm.stock" name="stock" min="0" required>
              </div>
              <div class="form-group">
                <label>Brand</label>
                <input type="text" [(ngModel)]="productForm.brand" name="brand">
              </div>
            </div>

            <div class="form-section">
              <h4>Images</h4>
              <div class="image-upload">
                <input type="file" #fileInput multiple accept="image/*" (change)="onFilesSelected($event)" style="display: none" [attr.id]="'file-' + modalId()">
                <label for="file-{{modalId()}}" class="btn btn-secondary">Select Images</label>
                <span class="file-count">{{ selectedFiles.length }} files selected</span>
              </div>
              <div class="image-previews">
                @for (preview of imagePreviews(); track $index; let i = $index) {
                  <div class="image-preview">
                    <img [src]="preview" alt="Preview">
                    <button type="button" class="remove-btn" (click)="removeImagePreview(i)">×</button>
                  </div>
                }
              </div>
              <div class="existing-images">
                @for (img of productImages(); track img.id) {
                  <div class="image-preview">
                    <img [src]="getFullImageUrl(img.url || img.imageUrl)" alt="{{ img.altText || '' }}">
                    <button type="button" class="remove-btn danger" (click)="deleteProductImage(img.id)">×</button>
                  </div>
                }
              </div>
            </div>

            <div class="form-section">
              <div class="section-header">
                <h4>Variants</h4>
                <button type="button" class="btn btn-sm btn-secondary" (click)="openAddVariant()">Add Variant</button>
              </div>
              <div class="variants-list">
                @for (variant of productVariants(); track variant.id) {
                  <div class="variant-item">
                    <span>{{ variant.name }} - ₹{{ variant.price }} - Stock: {{ variant.stock }}</span>
                    <div class="variant-actions">
                      <button type="button" class="btn btn-sm" (click)="openEditVariant(variant)">Edit</button>
                      <button type="button" class="btn btn-sm danger" (click)="deleteVariant(variant)">Delete</button>
                    </div>
                  </div>
                } @empty {
                  <p class="empty-text">No variants added</p>
                }
              </div>
            </div>
          </form>
        </app-admin-modal>
      }

      @if (showVariantModal()) {
        <app-admin-modal
          [isOpen]="showVariantModal()"
          [title]="editingVariant() ? 'Edit Variant' : 'Add Variant'"
          (close)="closeVariantModal()"
          (confirm)="saveVariant()">
          <div class="variant-form">
            <div class="form-group">
              <label>Name *</label>
              <input type="text" [(ngModel)]="variantForm.name" name="vName" required>
            </div>
            <div class="form-row">
              <div class="form-group">
                <label>Price *</label>
                <input type="number" [(ngModel)]="variantForm.price" name="vPrice" step="0.01" min="0" required>
              </div>
              <div class="form-group">
                <label>Stock *</label>
                <input type="number" [(ngModel)]="variantForm.stock" name="vStock" min="0" required>
              </div>
            </div>
            <div class="form-group">
              <label>Sort Order</label>
              <input type="number" [(ngModel)]="variantForm.sortOrder" name="vSortOrder" min="0">
            </div>
          </div>
        </app-admin-modal>
      }
    </div>
  `,
  styles: [`
    .admin-products { padding: 1.5rem; }
    .section-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1.5rem; }
    .section-header h2 { margin: 0; font-size: 1.5rem; font-weight: 600; }
    .filters { display: flex; gap: 1rem; margin-bottom: 1.5rem; flex-wrap: wrap; }
    .search-input { flex: 1; min-width: 200px; padding: 0.5rem; border: 1px solid var(--border-color, #ddd); border-radius: 6px; }
    .filter-select { padding: 0.5rem; border: 1px solid var(--border-color, #ddd); border-radius: 6px; background: white; }

    .btn { padding: 0.625rem 1.25rem; border: none; border-radius: 6px; font-weight: 500; cursor: pointer; transition: all 0.2s; }
    .btn:disabled { opacity: 0.6; cursor: not-allowed; }
    .btn-primary { background: var(--primary, #2874F0); color: white; }
    .btn-secondary { background: var(--secondary, #6c757d); color: white; }
    .btn-danger { background: var(--danger, #dc3545); color: white; }
    .btn-sm { padding: 0.375rem 0.75rem; font-size: 0.875rem; }

    .product-form { display: flex; flex-direction: column; gap: 1rem; }
    .form-row { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; }
    .form-group { display: flex; flex-direction: column; gap: 0.375rem; }
    .form-group label { font-weight: 500; font-size: 0.875rem; }
    .form-group input, .form-group select, .form-group textarea {
      padding: 0.5rem; border: 1px solid var(--border-color, #ddd); border-radius: 6px; font-size: 1rem;
    }
    .form-section { padding-top: 1rem; border-top: 1px solid var(--border-color, #eee); }
    .form-section h4 { margin: 0 0 1rem; font-size: 1rem; font-weight: 600; }
    .section-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem; }
    .section-header h4 { margin: 0; }
    .image-upload { display: flex; align-items: center; gap: 1rem; margin-bottom: 1rem; }
    .file-count { color: var(--on-surface-variant, #666); font-size: 0.875rem; }
    .image-previews, .existing-images { display: flex; gap: 0.75rem; flex-wrap: wrap; margin-bottom: 1rem; }
    .image-preview { position: relative; width: 80px; height: 80px; border-radius: 8px; overflow: hidden; border: 1px solid var(--border-color, #eee); }
    .image-preview img { width: 100%; height: 100%; object-fit: cover; }
    .remove-btn { position: absolute; top: 4px; right: 4px; width: 24px; height: 24px; border: none; border-radius: 50%; background: rgba(0,0,0,0.6); color: white; cursor: pointer; font-size: 1rem; line-height: 1; }
    .remove-btn.danger { background: var(--danger, #dc3545); }
    .variants-list { display: flex; flex-direction: column; gap: 0.5rem; }
    .variant-item { display: flex; justify-content: space-between; align-items: center; padding: 0.75rem; background: var(--surface-variant, #fafafa); border-radius: 6px; }
    .variant-actions { display: flex; gap: 0.5rem; }
    .empty-text { color: var(--on-surface-variant, #666); text-align: center; padding: 1rem; margin: 0; }
  `]
})
export class AdminProductsComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly productService = inject(ProductService);
  private readonly categoryService = inject(CategoryService);
  private readonly notificationService = inject(NotificationService);

  products = signal<Product[]>([]);
  categories = signal<Category[]>([]);
  filteredProducts = signal<Product[]>([]);
  search = '';
  categoryFilter = '';

  showProductModal = signal(false);
  editingProduct = signal<Product | null>(null);
  productForm: CreateProduct = { name: '', description: '', price: 0, originalPrice: 0, stock: 0, category: '', brand: '' };
  productImages = signal<ProductImage[]>([]);
  selectedFiles: File[] = [];
  imagePreviews = signal<string[]>([]);
  modalId = signal(Date.now());

  productVariants = signal<ProductVariant[]>([]);
  showVariantModal = signal(false);
  editingVariant = signal<ProductVariant | null>(null);
  variantForm: CreateProductVariant = { name: '', price: 0, stock: 0, sortOrder: 0 };

  saving = signal(false);
  createdProductId: number | null = null;

  productColumns: ColumnDef<Product>[] = [
    { key: 'id', header: 'ID', render: (p) => `#${p.id}` },
    { key: 'name', header: 'Name' },
    { key: 'category', header: 'Category' },
    { key: 'price', header: 'Price', render: (p) => `₹${p.price.toLocaleString('en-IN')}` },
    { key: 'stock', header: 'Stock' },
    { key: 'brand', header: 'Brand' }
  ];

  productActions: ActionDef<Product>[] = [
    { label: 'Edit', action: (p) => this.openEditProduct(p), class: 'primary' },
    { label: 'Delete', action: (p) => this.deleteProduct(p), class: 'danger' }
  ];

  ngOnInit(): void {
    this.loadProducts();
    this.loadCategories();
  }

  loadProducts(): void {
    this.productService.getAll().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        this.products.set(data);
        this.filterProducts();
      }
    });
  }

  loadCategories(): void {
    this.categoryService.getAll().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => this.categories.set(data)
    });
  }

  onSearch(): void {
    this.filterProducts();
  }

  filterProducts(): void {
    let items = this.products();
    if (this.search) {
      const term = this.search.toLowerCase();
      items = items.filter(p =>
        p.name.toLowerCase().includes(term) ||
        p.description.toLowerCase().includes(term) ||
        (p.brand?.toLowerCase().includes(term))
      );
    }
    if (this.categoryFilter) {
      items = items.filter(p => p.category == this.categoryFilter);
    }
    this.filteredProducts.set(items);
  }

  openAddProduct(): void {
    this.editingProduct.set(null);
    this.productForm = { name: '', description: '', price: 0, originalPrice: 0, stock: 0, category: '', brand: '' };
    this.productImages.set([]);
    this.selectedFiles = [];
    this.imagePreviews.set([]);
    this.productVariants.set([]);
    this.modalId.set(Date.now());
    this.showProductModal.set(true);
  }

  openEditProduct(product: Product): void {
    this.editingProduct.set(product);
    this.productForm = {
      name: product.name,
      description: product.description,
      price: product.price,
      originalPrice: product.originalPrice || 0,
      stock: product.stock,
      category: product.category,
      brand: product.brand || ''
    };
    this.productImages.set(product.images || []);
    this.selectedFiles = [];
    this.imagePreviews.set([]);
    this.productVariants.set(product.variants || []);
    this.modalId.set(Date.now());
    this.showProductModal.set(true);
  }

  closeProductModal(): void {
    this.showProductModal.set(false);
    this.editingProduct.set(null);
    this.createdProductId = null;
  }

  onFilesSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files) {
      for (let i = 0; i < input.files.length; i++) {
        this.selectedFiles.push(input.files[i]);
        const reader = new FileReader();
        reader.onload = () => this.imagePreviews.update(p => [...p, reader.result as string]);
        reader.readAsDataURL(input.files[i]);
      }
      input.value = '';
    }
  }

  removeImagePreview(index: number): void {
    this.selectedFiles.splice(index, 1);
    this.imagePreviews.update(p => p.filter((_, i) => i !== index));
  }

  deleteProductImage(imageId: number): void {
    this.productService.deleteImage(imageId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.productImages.update(imgs => imgs.filter(img => img.id !== imageId));
        this.notificationService.showSuccess('Image deleted');
      },
      error: () => this.notificationService.showError('Failed to delete image')
    });
  }

  saveProduct(): void {
    if (!this.productForm.name || this.productForm.price <= 0) {
      this.notificationService.showError('Name and valid price required');
      return;
    }
    this.saving.set(true);

    const finish = () => {
      this.saving.set(false);
      this.notificationService.showSuccess(this.editingProduct() ? 'Product updated' : 'Product added');
      this.closeProductModal();
      this.loadProducts();
    };

    if (this.editingProduct()) {
      const updateDto: UpdateProduct = {
        name: this.productForm.name,
        description: this.productForm.description,
        price: this.productForm.price,
        originalPrice: this.productForm.originalPrice || undefined,
        stock: this.productForm.stock,
        category: this.productForm.category,
        brand: this.productForm.brand || undefined
      };
      this.productService.update(this.editingProduct()!.id, updateDto).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: () => finish(),
        error: (err) => {
          this.saving.set(false);
          this.notificationService.showError(err.error?.error || 'Failed to update product');
        }
      });
    } else {
      const createDto: CreateProduct = { ...this.productForm };
      this.productService.create(createDto).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: (created) => {
          this.createdProductId = created.id;
          if (this.selectedFiles.length > 0) {
            this.uploadMultipleImages(created.id, finish);
          } else {
            finish();
          }
        },
        error: (err) => {
          this.saving.set(false);
          this.notificationService.showError(err.error?.error || 'Failed to add product');
        }
      });
    }
  }

  private uploadMultipleImages(productId: number, callback: () => void): void {
    const total = this.selectedFiles.length;
    let completed = 0;
    for (const file of this.selectedFiles) {
      const formData = new FormData();
      formData.append('file', file);
      this.productService.uploadImage(productId, formData).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: () => {
          completed++;
          if (completed === total) callback();
        },
        error: () => {
          completed++;
          if (completed === total) callback();
        }
      });
    }
  }

  updateStock(product: Product, change: number): void {
    const newStock = product.stock + change;
    if (newStock < 0) return;
    this.productService.update(product.id, { ...product, stock: newStock } as UpdateProduct).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.notificationService.showSuccess(`Stock updated to ${newStock}`);
        this.loadProducts();
      }
    });
  }

  deleteProduct(product: Product): void {
    if (!confirm('Are you sure you want to delete this product?')) return;
    this.productService.delete(product.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.notificationService.showSuccess('Product deleted');
        this.loadProducts();
      },
      error: (err) => this.notificationService.showError(err.error?.error || 'Failed to delete product')
    });
  }

  getFullImageUrl(path: string): string {
    return `http://localhost:5068${path}`;
  }

  openAddVariant(): void {
    this.editingVariant.set(null);
    this.variantForm = { name: '', price: 0, stock: 0, sortOrder: this.productVariants().length };
    this.showVariantModal.set(true);
  }

  openEditVariant(v: ProductVariant): void {
    this.editingVariant.set(v);
    this.variantForm = { name: v.name, price: v.price, stock: v.stock, sortOrder: v.sortOrder };
    this.showVariantModal.set(true);
  }

  closeVariantModal(): void {
    this.showVariantModal.set(false);
    this.editingVariant.set(null);
  }

  saveVariant(): void {
    const product = this.editingProduct();
    if (!product || !this.variantForm.name || this.variantForm.price <= 0) return;

    if (this.editingVariant()) {
      this.productService.updateVariant(product.id, this.editingVariant()!.id, this.variantForm).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: () => {
          this.notificationService.showSuccess('Variant updated');
          this.closeVariantModal();
          this.loadProductVariants(product.id);
        },
        error: () => this.notificationService.showError('Failed to update variant')
      });
    } else {
      this.productService.addVariant(product.id, this.variantForm).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: () => {
          this.notificationService.showSuccess('Variant added');
          this.closeVariantModal();
          this.loadProductVariants(product.id);
        },
        error: () => this.notificationService.showError('Failed to add variant')
      });
    }
  }

  deleteVariant(v: ProductVariant): void {
    const product = this.editingProduct();
    if (!product || !confirm('Delete this variant?')) return;
    this.productService.deleteVariant(product.id, v.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.notificationService.showSuccess('Variant deleted');
        this.productVariants.update(list => list.filter(x => x.id !== v.id));
      },
      error: () => this.notificationService.showError('Failed to delete variant')
    });
  }

  private loadProductVariants(productId: number): void {
    this.productService.getVariants(productId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (variants) => this.productVariants.set(variants),
      error: () => this.notificationService.showError('Failed to load variants')
    });
  }
}