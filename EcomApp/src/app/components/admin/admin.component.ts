import { Component, inject, OnInit, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { AuthService } from '../../services/auth.service';
import { NotificationService } from '../../services/notification.service';
import { CategoryService } from '../../services/category.service';
import { BannerService } from '../../services/banner.service';
import { Product, CreateProduct, UpdateProduct } from '../../models/product.model';
import { Order } from '../../models/order.model';
import { User } from '../../models/auth.model';
import { Category, CreateCategory } from '../../models/category.model';
import { Banner, CreateBanner, UpdateBanner } from '../../models/banner.model';

interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

@Component({
  selector: 'app-admin',
  imports: [FormsModule, DatePipe],
  templateUrl: './admin.component.html',
  styleUrl: './admin.component.scss'
})
export class AdminComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly authService = inject(AuthService);
  readonly notificationService = inject(NotificationService);
  private readonly router = inject(Router);
  private readonly categoryService = inject(CategoryService);
  private readonly bannerService = inject(BannerService);
  private readonly apiUrl = 'http://localhost:5068/api';

  activeTab = signal<'dashboard' | 'products' | 'orders' | 'users' | 'categories' | 'banners'>('dashboard');

  stats = signal({ totalProducts: 0, totalOrders: 0, totalUsers: 0, totalRevenue: 0 });
  products = signal<Product[]>([]);
  orders = signal<Order[]>([]);
  users = signal<User[]>([]);

  orderPage = signal(1);
  userPage = signal(1);
  orderStatusFilter = signal<string>('');
  productSearch = signal('');
  productCategoryFilter = signal('');

  isLoading = signal(false);

  showProductModal = signal(false);
  editingProduct: Product | null = null;
  productForm: CreateProduct = { name: '', description: '', price: 0, stock: 0, category: '' };
  selectedFile: File | null = null;
  imagePreview: string | null = null;

  categories = signal<Category[]>([]);
  showCategoryModal = signal(false);
  editingCategory: Category | null = null;
  categoryForm: CreateCategory = { name: '', icon: '' };

  banners = signal<Banner[]>([]);
  showBannerModal = signal(false);
  editingBanner: Banner | null = null;
  bannerForm: CreateBanner = { title: '', subtitle: '', bgGradient: 'linear-gradient(135deg, #2874F0, #1a5dc8)', icon: 'devices', startDate: new Date().toISOString().split('T')[0], durationDays: 7, sortOrder: 1, isActive: true };
  bannerSelectedFile: File | null = null;
  bannerImagePreview: string | null = null;

  ngOnInit(): void {
    if (!this.authService.isAdmin()) {
      this.notificationService.showError('Access denied. Admin only.');
      this.router.navigate(['/products']);
      return;
    }
    this.loadDashboard();
  }

  setTab(tab: 'dashboard' | 'products' | 'orders' | 'users' | 'categories' | 'banners'): void {
    this.activeTab.set(tab);
    if (tab === 'orders') this.loadOrders();
    if (tab === 'users') this.loadUsers();
    if (tab === 'products') this.loadProducts();
    if (tab === 'categories') this.loadCategories();
    if (tab === 'banners') this.loadBanners();
  }

  loadDashboard(): void {
    this.isLoading.set(true);
    this.http.get<PaginatedResponse<Product>>(`${this.apiUrl}/products?pageNumber=1&pageSize=1000`).subscribe({
      next: (res) => {
        this.products.set(res.items);
        this.stats.update(s => ({ ...s, totalProducts: res.totalCount }));
      },
      complete: () => {
        this.http.get<any>(`${this.apiUrl}/orders/all?pageNumber=1&pageSize=1000`).subscribe({
          next: (res) => {
            this.orders.set(res.items);
            this.stats.update(s => ({
              ...s,
              totalOrders: res.totalCount,
              totalRevenue: res.items.reduce((sum: number, o: Order) => sum + o.totalAmount, 0)
            }));
          },
          complete: () => {
            this.http.get<PaginatedResponse<User>>(`${this.apiUrl}/auth/users?pageNumber=1&pageSize=1000`).subscribe({
              next: (res) => this.stats.update(s => ({ ...s, totalUsers: res.totalCount })),
              complete: () => this.isLoading.set(false)
            });
          }
        });
      }
    });
  }

  loadProducts(): void {
    this.isLoading.set(true);
    this.http.get<PaginatedResponse<Product>>(`${this.apiUrl}/products?pageNumber=1&pageSize=1000`).subscribe({
      next: (res) => {
        this.products.set(res.items);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  loadOrders(): void {
    this.isLoading.set(true);
    const status = this.orderStatusFilter();
    let url = `${this.apiUrl}/orders/all?pageNumber=${this.orderPage()}&pageSize=20`;
    if (status) url += `&status=${status}`;

    this.http.get<any>(url).subscribe({
      next: (res) => {
        this.orders.set(res.items);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  loadUsers(): void {
    this.isLoading.set(true);
    this.http.get<PaginatedResponse<User>>(`${this.apiUrl}/auth/users?pageNumber=${this.userPage()}&pageSize=20`).subscribe({
      next: (res) => {
        this.users.set(res.items);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  updateOrderStatus(orderId: number, status: string): void {
    this.http.put(`${this.apiUrl}/orders/${orderId}/status`, { status }).subscribe({
      next: () => {
        this.notificationService.showSuccess('Order status updated');
        this.loadOrders();
      },
      error: (err) => this.notificationService.showError(err.error?.error || 'Failed to update order')
    });
  }

  toggleUserStatus(user: User): void {
    const action = user.isActive ? 'deactivate' : 'activate';
    this.http.put(`${this.apiUrl}/auth/users/${user.id}/${action}`, {}).subscribe({
      next: () => {
        this.notificationService.showSuccess(`User ${action}d`);
        this.loadUsers();
      },
      error: (err) => this.notificationService.showError(err.error?.error || `Failed to ${action} user`)
    });
  }

  openAddProduct(): void {
    this.editingProduct = null;
    this.productForm = { name: '', description: '', price: 0, stock: 0, category: '' };
    this.selectedFile = null;
    this.imagePreview = null;
    this.showProductModal.set(true);
  }

  openEditProduct(product: Product): void {
    this.editingProduct = product;
    this.productForm = { name: product.name, description: product.description, price: product.price, stock: product.stock, category: product.category };
    this.selectedFile = null;
    this.imagePreview = product.imageUrl ? this.getFullImageUrl(product.imageUrl) : null;
    this.showProductModal.set(true);
  }

  closeProductModal(): void {
    this.showProductModal.set(false);
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

  saveProduct(): void {
    if (!this.productForm.name || this.productForm.price <= 0) {
      this.notificationService.showError('Name and valid price required');
      return;
    }

    if (this.editingProduct) {
      const updateDto: UpdateProduct = {
        name: this.productForm.name,
        description: this.productForm.description,
        price: this.productForm.price,
        stock: this.productForm.stock,
        category: this.productForm.category
      };
      this.http.put<Product>(`${this.apiUrl}/products/${this.editingProduct.id}`, updateDto).subscribe({
        next: (updated) => {
          if (this.selectedFile) {
            this.uploadImage(updated.id);
          } else {
            this.notificationService.showSuccess('Product updated');
            this.closeProductModal();
            this.loadProducts();
            this.loadDashboard();
          }
        },
        error: (err) => this.notificationService.showError(err.error?.error || 'Failed to update product')
      });
    } else {
      this.http.post<Product>(`${this.apiUrl}/products`, this.productForm).subscribe({
        next: (created) => {
          if (this.selectedFile) {
            this.uploadImage(created.id);
          } else {
            this.notificationService.showSuccess('Product added');
            this.closeProductModal();
            this.loadProducts();
            this.loadDashboard();
          }
        },
        error: (err) => this.notificationService.showError(err.error?.error || 'Failed to add product')
      });
    }
  }

  updateStock(product: Product, change: number): void {
    const newStock = product.stock + change;
    if (newStock < 0) return;
    const updateDto: UpdateProduct = {
      name: product.name,
      description: product.description,
      price: product.price,
      stock: newStock,
      category: product.category
    };
    this.http.put<Product>(`${this.apiUrl}/products/${product.id}`, updateDto).subscribe({
      next: () => {
        this.notificationService.showSuccess(`Stock updated to ${newStock}`);
        this.loadProducts();
        this.loadDashboard();
      },
      error: (err) => this.notificationService.showError(err.error?.error || 'Failed to update stock')
    });
  }

  deleteProduct(id: number): void {
    if (!confirm('Are you sure you want to delete this product?')) return;
    this.http.delete(`${this.apiUrl}/products/${id}`).subscribe({
      next: () => {
        this.notificationService.showSuccess('Product deleted');
        this.products.update(p => p.filter(x => x.id !== id));
        this.stats.update(s => ({ ...s, totalProducts: s.totalProducts - 1 }));
      },
      error: (err) => this.notificationService.showError(err.error?.error || 'Failed to delete product')
    });
  }

  private uploadImage(productId: number): void {
    const formData = new FormData();
    formData.append('file', this.selectedFile!);
    this.http.post<Product>(`${this.apiUrl}/products/${productId}/image`, formData).subscribe({
      next: () => {
        this.notificationService.showSuccess('Product saved with image');
        this.closeProductModal();
        this.loadProducts();
        this.loadDashboard();
      },
      error: () => {
        this.notificationService.showSuccess('Product saved, image upload failed');
        this.closeProductModal();
        this.loadProducts();
        this.loadDashboard();
      }
    });
  }

  getFullImageUrl(path: string): string {
    return `http://localhost:5068${path}`;
  }

  getStatusClass(status: string): string {
    const map: Record<string, string> = {
      Pending: 'status-pending',
      Processing: 'status-processing',
      Shipped: 'status-shipped',
      Delivered: 'status-delivered',
      Cancelled: 'status-cancelled'
    };
    return map[status] || '';
  }

  loadCategories(): void {
    this.categoryService.getAll().subscribe({
      next: (data) => this.categories.set(data),
      error: () => this.notificationService.showError('Failed to load categories')
    });
  }

  openAddCategory(): void {
    this.editingCategory = null;
    this.categoryForm = { name: '', icon: '' };
    this.showCategoryModal.set(true);
  }

  openEditCategory(cat: Category): void {
    this.editingCategory = cat;
    this.categoryForm = { name: cat.name, icon: cat.icon ?? '' };
    this.showCategoryModal.set(true);
  }

  closeCategoryModal(): void {
    this.showCategoryModal.set(false);
    this.editingCategory = null;
    this.categoryForm = { name: '', icon: '' };
  }

  saveCategory(): void {
    if (!this.categoryForm.name) {
      this.notificationService.showError('Category name is required');
      return;
    }
    const iconVal = this.categoryForm.icon || undefined;
    if (this.editingCategory) {
      this.categoryService.update(this.editingCategory.id, { name: this.categoryForm.name, icon: iconVal }).subscribe({
        next: () => {
          this.notificationService.showSuccess('Category updated');
          this.closeCategoryModal();
          this.loadCategories();
        },
        error: (err) => this.notificationService.showError(err.error?.error || 'Failed to update category')
      });
    } else {
      this.categoryService.create({ name: this.categoryForm.name, icon: iconVal }).subscribe({
        next: () => {
          this.notificationService.showSuccess('Category created');
          this.closeCategoryModal();
          this.loadCategories();
        },
        error: (err) => this.notificationService.showError(err.error?.error || 'Failed to create category')
      });
    }
  }

  deleteCategory(id: number): void {
    if (!confirm('Delete this category? Products using it will keep the category name but it will be removed from the list.')) return;
    this.categoryService.delete(id).subscribe({
      next: () => {
        this.notificationService.showSuccess('Category deleted');
        this.categories.update(c => c.filter(x => x.id !== id));
      },
      error: (err) => this.notificationService.showError(err.error?.error || 'Failed to delete category')
    });
  }

  loadBanners(): void {
    this.bannerService.getAll().subscribe({
      next: (data) => this.banners.set(data),
      error: () => this.notificationService.showError('Failed to load banners')
    });
  }

  openAddBanner(): void {
    this.editingBanner = null;
    this.bannerForm = { title: '', subtitle: '', bgGradient: 'linear-gradient(135deg, #2874F0, #1a5dc8)', icon: 'devices', startDate: new Date().toISOString().split('T')[0], durationDays: 7, sortOrder: 1, isActive: true };
    this.bannerSelectedFile = null;
    this.bannerImagePreview = null;
    this.showBannerModal.set(true);
  }

  openEditBanner(banner: Banner): void {
    this.editingBanner = banner;
    this.bannerForm = {
      title: banner.title,
      subtitle: banner.subtitle,
      bgGradient: banner.bgGradient,
      icon: banner.icon,
      startDate: banner.startDate.split('T')[0] || banner.startDate,
      durationDays: banner.durationDays,
      sortOrder: banner.sortOrder,
      isActive: banner.isActive
    };
    this.bannerSelectedFile = null;
    this.bannerImagePreview = banner.imageUrl ? this.getFullImageUrl(banner.imageUrl) : null;
    this.showBannerModal.set(true);
  }

  closeBannerModal(): void {
    this.showBannerModal.set(false);
    this.editingBanner = null;
    this.bannerForm = { title: '', subtitle: '', bgGradient: 'linear-gradient(135deg, #2874F0, #1a5dc8)', icon: 'devices', startDate: new Date().toISOString().split('T')[0], durationDays: 7, sortOrder: 1, isActive: true };
    this.bannerSelectedFile = null;
    this.bannerImagePreview = null;
  }

  onBannerFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.bannerSelectedFile = input.files[0];
      const reader = new FileReader();
      reader.onload = () => this.bannerImagePreview = reader.result as string;
      reader.readAsDataURL(this.bannerSelectedFile);
    }
  }

  saveBanner(): void {
    if (!this.bannerForm.title) {
      this.notificationService.showError('Banner title is required');
      return;
    }
    const payload: CreateBanner = {
      ...this.bannerForm,
      startDate: new Date(this.bannerForm.startDate).toISOString()
    };
    if (this.editingBanner) {
      this.bannerService.update(this.editingBanner.id, payload as UpdateBanner).subscribe({
        next: (updated) => {
          if (this.bannerSelectedFile) {
            this.uploadBannerImage(updated.id);
          } else {
            this.notificationService.showSuccess('Banner updated');
            this.closeBannerModal();
            this.loadBanners();
          }
        },
        error: (err) => this.notificationService.showError(err.error?.error || 'Failed to update banner')
      });
    } else {
      this.bannerService.create(payload).subscribe({
        next: (created) => {
          if (this.bannerSelectedFile) {
            this.uploadBannerImage(created.id);
          } else {
            this.notificationService.showSuccess('Banner created');
            this.closeBannerModal();
            this.loadBanners();
          }
        },
        error: (err) => this.notificationService.showError(err.error?.error || 'Failed to create banner')
      });
    }
  }

  private uploadBannerImage(bannerId: number): void {
    const formData = new FormData();
    formData.append('file', this.bannerSelectedFile!);
    this.http.post<Banner>(`${this.apiUrl}/banners/${bannerId}/image`, formData).subscribe({
      next: () => {
        this.notificationService.showSuccess('Banner saved with image');
        this.closeBannerModal();
        this.loadBanners();
      },
      error: () => {
        this.notificationService.showSuccess('Banner saved, image upload failed');
        this.closeBannerModal();
        this.loadBanners();
      }
    });
  }

  deleteBanner(id: number): void {
    if (!confirm('Delete this banner?')) return;
    this.bannerService.delete(id).subscribe({
      next: () => {
        this.notificationService.showSuccess('Banner deleted');
        this.banners.update(b => b.filter(x => x.id !== id));
      },
      error: (err) => this.notificationService.showError(err.error?.error || 'Failed to delete banner')
    });
  }

  toggleBannerStatus(banner: Banner): void {
    const updated: UpdateBanner = {
      title: banner.title,
      subtitle: banner.subtitle,
      bgGradient: banner.bgGradient,
      icon: banner.icon,
      startDate: banner.startDate,
      durationDays: banner.durationDays,
      sortOrder: banner.sortOrder,
      isActive: !banner.isActive
    };
    this.bannerService.update(banner.id, updated).subscribe({
      next: () => {
        this.notificationService.showSuccess(`Banner ${updated.isActive ? 'activated' : 'deactivated'}`);
        this.loadBanners();
      },
      error: (err) => this.notificationService.showError(err.error?.error || 'Failed to toggle banner')
    });
  }

  bannerEndDate(banner: Banner): string {
    const start = new Date(banner.startDate);
    const end = new Date(start.getTime() + banner.durationDays * 24 * 60 * 60 * 1000);
    return end.toLocaleDateString('en-IN');
  }

  bannerStatus(banner: Banner): 'active' | 'inactive' | 'expired' {
    if (!banner.isActive) return 'inactive';
    const now = new Date();
    const start = new Date(banner.startDate);
    const end = new Date(start.getTime() + banner.durationDays * 24 * 60 * 60 * 1000);
    if (now < start) return 'inactive';
    if (now > end) return 'expired';
    return 'active';
  }

  getFilteredProducts(): Product[] {
    let items = this.products();
    const search = this.productSearch().toLowerCase();
    const category = this.productCategoryFilter();
    if (search) items = items.filter(p => p.name.toLowerCase().includes(search) || p.description.toLowerCase().includes(search));
    if (category) items = items.filter(p => p.category === category);
    return items;
  }
}
