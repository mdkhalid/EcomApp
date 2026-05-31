import { Component, inject, OnInit, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { AuthService } from '../../services/auth.service';
import { NotificationService } from '../../services/notification.service';
import { CategoryService } from '../../services/category.service';
import { BannerService } from '../../services/banner.service';
import { CouponService } from '../../services/coupon.service';
import { Product, CreateProduct, UpdateProduct } from '../../models/product.model';
import { Order } from '../../models/order.model';
import { User, CreateUserRequest, AdminChangePasswordRequest } from '../../models/auth.model';
import { Category, CreateCategory } from '../../models/category.model';
import { Banner, CreateBanner, UpdateBanner } from '../../models/banner.model';
import { Coupon, CreateCoupon } from '../../models/coupon.model';

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
  private readonly couponService = inject(CouponService);
  private readonly apiUrl = 'http://localhost:5068/api';

  activeTab = signal<'dashboard' | 'products' | 'orders' | 'users' | 'categories' | 'banners' | 'coupons'>('dashboard');

  stats = signal({ totalProducts: 0, totalOrders: 0, totalUsers: 0, totalRevenue: 0 });
  products = signal<Product[]>([]);
  orders = signal<Order[]>([]);
  users = signal<User[]>([]);
  totalUsersCount = signal(0);
  userPageSize = 20;

  orderPage = signal(1);
  userPage = signal(1);
  userSearch = signal('');
  userRoleFilter = signal('');
  orderStatusFilter = '';
  productSearch = signal('');
  productCategoryFilter = signal('');

  isLoading = signal(false);

  showProductModal = signal(false);
  editingProduct: Product | null = null;
  productForm = { name: '', description: '', price: 0, originalPrice: 0, stock: 0, category: '', brand: '' };
  selectedFile: File | null = null;
  imagePreview: string | null = null;

  categories = signal<Category[]>([]);
  showCategoryModal = signal(false);
  editingCategory: Category | null = null;
  categoryForm: CreateCategory = { name: '', icon: '' };

  // Create User
  showCreateUserModal = signal(false);
  createUserForm: CreateUserRequest = { email: '', username: '', password: '', confirmPassword: '', role: 'Customer', firstName: '', lastName: '', phone: '' };
  availableRoles = signal<string[]>(['Customer', 'SubAdmin', 'Admin']);

  // Change Password
  showChangePasswordModal = signal(false);
  changePasswordUser = signal<User | null>(null);
  changePasswordForm: AdminChangePasswordRequest = { newPassword: '', confirmPassword: '' };

  banners = signal<Banner[]>([]);
  showBannerModal = signal(false);
  editingBanner: Banner | null = null;
  bannerForm: CreateBanner = { title: '', subtitle: '', bgGradient: 'linear-gradient(135deg, #2874F0, #1a5dc8)', icon: 'devices', startDate: new Date().toISOString().split('T')[0], durationDays: 7, sortOrder: 1, isActive: true };
  bannerSelectedFile: File | null = null;
  bannerImagePreview: string | null = null;

  coupons = signal<Coupon[]>([]);
  showCouponModal = signal(false);
  editingCoupon: Coupon | null = null;
  couponForm: CreateCoupon = { code: '', description: '', type: 'Percentage', value: 10, minCartValue: 0, maxUses: 0, expiresAt: '', isActive: true };

  ngOnInit(): void {
    if (!this.authService.isAdmin()) {
      this.notificationService.showError('Access denied. Admin only.');
      this.router.navigate(['/products']);
      return;
    }
    this.loadDashboard();
  }

  setTab(tab: 'dashboard' | 'products' | 'orders' | 'users' | 'categories' | 'banners' | 'coupons'): void {
    this.activeTab.set(tab);
    if (tab === 'orders') this.loadOrders();
    if (tab === 'users') { this.loadUsers(); this.loadRoles(); }
    if (tab === 'products') { this.loadProducts(); this.loadCategories(); }
    if (tab === 'categories') this.loadCategories();
    if (tab === 'banners') this.loadBanners();
    if (tab === 'coupons') this.loadCoupons();
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
    const status = this.orderStatusFilter;
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
    const search = this.userSearch();
    const role = this.userRoleFilter();
    let url = `${this.apiUrl}/auth/users?pageNumber=${this.userPage()}&pageSize=20`;
    if (search) url += `&search=${encodeURIComponent(search)}`;
    if (role) url += `&role=${encodeURIComponent(role)}`;
    this.http.get<PaginatedResponse<User>>(url).subscribe({
      next: (res) => {
        this.users.set(res.items);
        this.totalUsersCount.set(res.totalCount);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  get userTotalPages(): number {
    return Math.ceil(this.totalUsersCount() / this.userPageSize) || 1;
  }

  get userHasNextPage(): boolean {
    return this.userPage() < this.userTotalPages;
  }

  get userHasPrevPage(): boolean {
    return this.userPage() > 1;
  }

  get userPageStartItem(): number {
    return (this.userPage() - 1) * this.userPageSize + 1;
  }

  get userPageEndItem(): number {
    return Math.min(this.userPage() * this.userPageSize, this.totalUsersCount());
  }

  goToUserPage(page: number): void {
    if (page < 1 || page > this.userTotalPages) return;
    this.userPage.set(page);
    this.loadUsers();
  }

  loadRoles(): void {
    this.http.get<{ roles: string[] }>(`${this.apiUrl}/auth/users/roles`).subscribe({
      next: (res) => this.availableRoles.set(res.roles)
    });
  }

  // Create User
  openCreateUser(): void {
    this.createUserForm = { email: '', username: '', password: '', confirmPassword: '', role: 'Customer', firstName: '', lastName: '', phone: '' };
    this.showCreateUserModal.set(true);
  }

  closeCreateUser(): void {
    this.showCreateUserModal.set(false);
  }

  saveCreateUser(): void {
    const f = this.createUserForm;
    if (!f.email || !f.username || !f.password) {
      this.notificationService.showError('Email, username and password are required');
      return;
    }
    if (f.username.length < 3) {
      this.notificationService.showError('Username must be at least 3 characters');
      return;
    }
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(f.email)) {
      this.notificationService.showError('Please enter a valid email address');
      return;
    }
    if (f.password.length < 8) {
      this.notificationService.showError('Password must be at least 8 characters');
      return;
    }
    if (f.password !== f.confirmPassword) {
      this.notificationService.showError('Passwords do not match');
      return;
    }
    const payload = { ...f };
    if (!payload.phone) delete payload.phone;
    if (!payload.firstName) delete payload.firstName;
    if (!payload.lastName) delete payload.lastName;
    this.http.post<User>(`${this.apiUrl}/auth/users`, payload).subscribe({
      next: () => {
        this.notificationService.showSuccess('User created successfully');
        this.closeCreateUser();
        this.loadUsers();
      },
      error: (err) => this.notificationService.showError(err.error?.error || 'Failed to create user')
    });
  }

  // Change Password
  openChangePassword(user: User): void {
    this.changePasswordUser.set(user);
    this.changePasswordForm = { newPassword: '', confirmPassword: '' };
    this.showChangePasswordModal.set(true);
  }

  closeChangePassword(): void {
    this.showChangePasswordModal.set(false);
    this.changePasswordUser.set(null);
  }

  saveChangePassword(): void {
    const f = this.changePasswordForm;
    const userId = this.changePasswordUser()?.id;
    if (!userId) return;

    if (f.newPassword.length < 8) {
      this.notificationService.showError('Password must be at least 8 characters');
      return;
    }
    if (f.newPassword !== f.confirmPassword) {
      this.notificationService.showError('Passwords do not match');
      return;
    }
    this.http.post(`${this.apiUrl}/auth/users/${userId}/change-password`, f).subscribe({
      next: () => {
        this.notificationService.showSuccess('Password changed successfully');
        this.closeChangePassword();
      },
      error: (err) => this.notificationService.showError(err.error?.error || 'Failed to change password')
    });
  }

  isCurrentUser(user: User): boolean {
    return this.authService.currentUser()?.email === user.email;
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
    this.productForm = { name: '', description: '', price: 0, originalPrice: 0, stock: 0, category: '', brand: '' };
    this.selectedFile = null;
    this.imagePreview = null;
    this.showProductModal.set(true);
  }

  openEditProduct(product: Product): void {
    this.editingProduct = product;
    this.productForm = { name: product.name, description: product.description, price: product.price, originalPrice: product.originalPrice || 0, stock: product.stock, category: product.category, brand: product.brand || '' };
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
        originalPrice: this.productForm.originalPrice || undefined,
        stock: this.productForm.stock,
        category: this.productForm.category,
        brand: this.productForm.brand || undefined
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
      const createDto: CreateProduct = {
        name: this.productForm.name,
        description: this.productForm.description,
        price: this.productForm.price,
        originalPrice: this.productForm.originalPrice || undefined,
        stock: this.productForm.stock,
        category: this.productForm.category,
        brand: this.productForm.brand || undefined
      };
      this.http.post<Product>(`${this.apiUrl}/products`, createDto).subscribe({
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
      originalPrice: product.originalPrice || undefined,
      stock: newStock,
      category: product.category,
      brand: product.brand || undefined
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
    if (search) items = items.filter(p => p.name.toLowerCase().includes(search) || p.description.toLowerCase().includes(search) || (p.brand && p.brand.toLowerCase().includes(search)));
    if (category) items = items.filter(p => p.category === category);
    return items;
  }

  // Coupon Management
  loadCoupons(): void {
    this.isLoading.set(true);
    this.couponService.getAll().subscribe({
      next: (data) => {
        this.coupons.set(data);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  openAddCoupon(): void {
    this.editingCoupon = null;
    this.couponForm = { code: '', description: '', type: 'Percentage', value: 10, minCartValue: 0, maxUses: 0, expiresAt: '', isActive: true };
    this.showCouponModal.set(true);
  }

  openEditCoupon(coupon: Coupon): void {
    this.editingCoupon = coupon;
    this.couponForm = {
      code: coupon.code,
      description: coupon.description,
      type: coupon.type,
      value: coupon.value,
      minCartValue: coupon.minCartValue,
      maxUses: coupon.maxUses,
      expiresAt: coupon.expiresAt.split('T')[0],
      isActive: coupon.isActive
    };
    this.showCouponModal.set(true);
  }

  closeCouponModal(): void {
    this.showCouponModal.set(false);
    this.editingCoupon = null;
    this.couponForm = { code: '', description: '', type: 'Percentage', value: 10, minCartValue: 0, maxUses: 0, expiresAt: '', isActive: true };
  }

  saveCoupon(): void {
    const f = this.couponForm;
    if (!f.code || !f.value || !f.expiresAt) {
      this.notificationService.showError('Code, value and expiry date are required');
      return;
    }
    if (f.code.length < 2) {
      this.notificationService.showError('Code must be at least 2 characters');
      return;
    }
    if (f.type === 'Percentage' && f.value > 100) {
      this.notificationService.showError('Percentage discount cannot exceed 100%');
      return;
    }
    const payload: CreateCoupon = {
      ...f,
      expiresAt: new Date(f.expiresAt).toISOString()
    };
    if (this.editingCoupon) {
      this.couponService.update(this.editingCoupon.id, payload).subscribe({
        next: () => {
          this.notificationService.showSuccess('Coupon updated');
          this.closeCouponModal();
          this.loadCoupons();
        },
        error: (err) => this.notificationService.showError(err.error?.error || 'Failed to update coupon')
      });
    } else {
      this.couponService.create(payload).subscribe({
        next: () => {
          this.notificationService.showSuccess('Coupon created');
          this.closeCouponModal();
          this.loadCoupons();
        },
        error: (err) => this.notificationService.showError(err.error?.error || 'Failed to create coupon')
      });
    }
  }

  deleteCoupon(id: number): void {
    if (!confirm('Delete this coupon?')) return;
    this.couponService.delete(id).subscribe({
      next: () => {
        this.notificationService.showSuccess('Coupon deleted');
        this.coupons.update(c => c.filter(x => x.id !== id));
      },
      error: (err) => this.notificationService.showError(err.error?.error || 'Failed to delete coupon')
    });
  }

  isCouponExpired(coupon: Coupon): boolean {
    return new Date(coupon.expiresAt) < new Date();
  }

  toggleCouponStatus(coupon: Coupon): void {
    const payload: CreateCoupon = {
      code: coupon.code,
      description: coupon.description,
      type: coupon.type,
      value: coupon.value,
      minCartValue: coupon.minCartValue,
      maxUses: coupon.maxUses,
      expiresAt: coupon.expiresAt,
      isActive: !coupon.isActive
    };
    this.couponService.update(coupon.id, payload).subscribe({
      next: () => {
        this.notificationService.showSuccess(`Coupon ${coupon.isActive ? 'deactivated' : 'activated'}`);
        this.loadCoupons();
      },
      error: (err) => this.notificationService.showError(err.error?.error || 'Failed to toggle coupon')
    });
  }
}
