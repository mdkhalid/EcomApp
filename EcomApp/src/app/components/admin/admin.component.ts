import { Component, ElementRef, inject, OnInit, signal, viewChild, AfterViewInit, OnDestroy } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DatePipe, DecimalPipe } from '@angular/common';
import { AuthService } from '../../services/auth.service';
import { NotificationService } from '../../services/notification.service';
import { CategoryService } from '../../services/category.service';
import { BannerService } from '../../services/banner.service';
import { CouponService } from '../../services/coupon.service';
import { AnalyticsService } from '../../services/analytics.service';
import { Product, CreateProduct, UpdateProduct, ProductImage, ProductVariant, CreateProductVariant } from '../../models/product.model';
import { Order } from '../../models/order.model';
import { User, CreateUserRequest, AdminChangePasswordRequest } from '../../models/auth.model';
import { Category, CreateCategory } from '../../models/category.model';
import { Banner, CreateBanner, UpdateBanner } from '../../models/banner.model';
import { Coupon, CreateCoupon } from '../../models/coupon.model';
import { ReturnRequest } from '../../models/return.model';
import { ReturnService } from '../../services/return.service';
import { AdminNotificationService } from '../../services/admin-notification.service';
import { AnalyticsOverview, CategoryBreakdown, OrderStatusBreakdown, RevenuePoint, RevenueSummary, TopProduct } from '../../models/analytics.model';
import { Chart, ChartConfiguration, registerables } from 'chart.js';

interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

Chart.register(...registerables);

@Component({
  selector: 'app-admin',
  imports: [FormsModule, DatePipe, DecimalPipe],
  templateUrl: './admin.component.html',
  styleUrl: './admin.component.scss'
})
export class AdminComponent implements OnInit, AfterViewInit, OnDestroy {
  private readonly http = inject(HttpClient);
  private readonly authService = inject(AuthService);
  readonly notificationService = inject(NotificationService);
  private readonly router = inject(Router);
  private readonly categoryService = inject(CategoryService);
  private readonly bannerService = inject(BannerService);
  private readonly couponService = inject(CouponService);
  private readonly returnService = inject(ReturnService);
  private readonly analyticsService = inject(AnalyticsService);
  readonly notifService = inject(AdminNotificationService);
  private readonly apiUrl = 'http://localhost:5068/api';

  activeTab = signal<'dashboard' | 'products' | 'orders' | 'users' | 'categories' | 'banners' | 'coupons' | 'returns' | 'analytics'>('dashboard');

  get isSuperAdmin(): boolean {
    return this.authService.isSuperAdmin();
  }

  revenueCanvas = viewChild<ElementRef<HTMLCanvasElement>>('revenueChart');
  topProductsCanvas = viewChild<ElementRef<HTMLCanvasElement>>('topProductsChart');
  categoryCanvas = viewChild<ElementRef<HTMLCanvasElement>>('categoryChart');
  orderStatusCanvas = viewChild<ElementRef<HTMLCanvasElement>>('orderStatusChart');

  analyticsOverview = signal<AnalyticsOverview | null>(null);
  revenue = signal<RevenueSummary | null>(null);
  topProducts = signal<TopProduct[]>([]);
  categoryBreakdown = signal<CategoryBreakdown[]>([]);
  orderStatusBreakdown = signal<OrderStatusBreakdown[]>([]);
  revenuePeriod = signal<'daily' | 'weekly' | 'monthly'>('monthly');
  analyticsLoading = signal(false);

  private revenueChart?: Chart;
  private topProductsChart?: Chart;
  private categoryChart?: Chart;
  private orderStatusChart?: Chart;

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
  productImages = signal<ProductImage[]>([]);
  selectedFiles: File[] = [];
  imagePreviews = signal<string[]>([]);

  productVariants = signal<ProductVariant[]>([]);
  showVariantModal = signal(false);
  editingVariant: ProductVariant | null = null;
  variantForm: CreateProductVariant = { name: '', price: 0, stock: 0, sortOrder: 0 };

  categories = signal<Category[]>([]);
  showCategoryModal = signal(false);
  editingCategory: Category | null = null;
  categoryForm: CreateCategory = { name: '', icon: '' };

  // Create User
  showCreateUserModal = signal(false);
  createUserForm: CreateUserRequest = { email: '', username: '', password: '', confirmPassword: '', role: 'Customer', firstName: '', lastName: '', phone: '' };
  createUserErrors: { [key: string]: string } = {};
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

  returns = signal<ReturnRequest[]>([]);
  totalReturns = signal(0);
  returnStatusFilter = '';
  returnPage = signal(1);
  showReturnDetailModal = signal(false);
  selectedReturn = signal<ReturnRequest | null>(null);
  returnAdminNote = signal('');
  returnDetailAction = signal('');

  ngOnInit(): void {
    if (!this.authService.isAdmin()) {
      this.notificationService.showError('Access denied. Admin only.');
      this.router.navigate(['/products']);
      return;
    }
    this.loadDashboard();
    this.notifService.loadCount();
    setInterval(() => this.notifService.loadCount(), 30000);
  }

  setTab(tab: 'dashboard' | 'products' | 'orders' | 'users' | 'categories' | 'banners' | 'coupons' | 'returns' | 'analytics'): void {
    this.activeTab.set(tab);
    if (tab === 'orders') this.loadOrders();
    if (tab === 'users') { this.loadUsers(); this.loadRoles(); }
    if (tab === 'products') { this.loadProducts(); this.loadCategories(); }
    if (tab === 'categories') this.loadCategories();
    if (tab === 'banners') this.loadBanners();
    if (tab === 'coupons') this.loadCoupons();
    if (tab === 'returns') this.loadReturns();
    if (tab === 'analytics') this.loadAnalytics();
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

  loadAnalytics(): void {
    this.analyticsLoading.set(true);
    this.analyticsService.getOverview().subscribe({
      next: (data) => {
        this.analyticsOverview.set(data);
        this.orderStatusBreakdown.set(data.orderStatusBreakdown);
        if (data.topProducts?.length) {
          this.topProducts.set(data.topProducts);
        }
        this.analyticsLoading.set(false);
        queueMicrotask(() => this.renderAllCharts());
        if (this.isSuperAdmin) {
          this.loadRevenue();
        }
      },
      error: () => {
        this.analyticsLoading.set(false);
        this.notificationService.showError('Failed to load analytics');
      }
    });
  }

  loadRevenue(): void {
    this.analyticsService.getRevenue(this.revenuePeriod()).subscribe({
      next: (data) => {
        this.revenue.set(data);
        queueMicrotask(() => this.renderRevenueChart());
      },
      error: () => this.notificationService.showError('Failed to load revenue')
    });
  }

  loadTopProductsAndCategory(): void {
    this.analyticsService.getTopProducts(10).subscribe({
      next: (data) => {
        this.topProducts.set(data);
        queueMicrotask(() => this.renderTopProductsChart());
      },
      error: () => this.notificationService.showError('Failed to load top products')
    });
    this.analyticsService.getCategoryBreakdown().subscribe({
      next: (data) => {
        this.categoryBreakdown.set(data);
        queueMicrotask(() => this.renderCategoryChart());
      },
      error: () => this.notificationService.showError('Failed to load category breakdown')
    });
  }

  private renderAllCharts(): void {
    this.renderOrderStatusChart();
    this.renderTopProductsChart();
    this.renderCategoryChart();
  }

  onRevenuePeriodChange(period: 'daily' | 'weekly' | 'monthly'): void {
    this.revenuePeriod.set(period);
    this.loadRevenue();
  }

  ngAfterViewInit(): void {
    if (this.activeTab() === 'analytics') {
      this.loadAnalytics();
    }
  }

  ngOnDestroy(): void {
    this.revenueChart?.destroy();
    this.topProductsChart?.destroy();
    this.categoryChart?.destroy();
    this.orderStatusChart?.destroy();
  }

  private renderRevenueChart(): void {
    const canvas = this.revenueCanvas()?.nativeElement;
    if (!canvas) return;
    const data = this.revenue();
    if (!data) return;

    this.revenueChart?.destroy();
    const config: ChartConfiguration<'line'> = {
      type: 'line',
      data: {
        labels: data.points.map(p => p.label),
        datasets: [{
          label: 'Revenue (₹)',
          data: data.points.map(p => p.revenue),
          borderColor: '#2874F0',
          backgroundColor: 'rgba(40, 116, 240, 0.12)',
          fill: true,
          tension: 0.35,
          pointRadius: 4,
          pointHoverRadius: 6,
          borderWidth: 2
        }, {
          label: 'Orders',
          data: data.points.map(p => p.orderCount),
          borderColor: '#fb641b',
          backgroundColor: 'transparent',
          tension: 0.35,
          pointRadius: 3,
          pointHoverRadius: 5,
          borderWidth: 2,
          yAxisID: 'y1'
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        interaction: { mode: 'index', intersect: false },
        plugins: {
          legend: { position: 'top' },
          tooltip: {
            callbacks: {
              label: (ctx) => {
                const v = ctx.parsed.y ?? 0;
                if (ctx.dataset.label?.startsWith('Revenue')) {
                  return `Revenue: ₹${v.toLocaleString('en-IN')}`;
                }
                return `Orders: ${v}`;
              }
            }
          }
        },
        scales: {
          y: {
            beginAtZero: true,
            ticks: {
              callback: (v) => '₹' + Number(v).toLocaleString('en-IN')
            }
          },
          y1: {
            beginAtZero: true,
            position: 'right',
            grid: { drawOnChartArea: false },
            ticks: { precision: 0 }
          }
        }
      }
    };
    this.revenueChart = new Chart(canvas, config);
  }

  private renderTopProductsChart(): void {
    const canvas = this.topProductsCanvas()?.nativeElement;
    if (!canvas) return;
    const items = this.topProducts();
    if (!items.length) return;

    this.topProductsChart?.destroy();
    const config: ChartConfiguration<'bar'> = {
      type: 'bar',
      data: {
        labels: items.map(p => this.truncate(p.productName, 22)),
        datasets: [{
          label: 'Units Sold',
          data: items.map(p => p.unitsSold),
          backgroundColor: 'rgba(56, 142, 60, 0.8)',
          borderRadius: 4
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        indexAxis: 'y',
        plugins: {
          legend: { display: false },
          tooltip: {
            callbacks: {
              label: (ctx) => {
                const item = items[ctx.dataIndex];
                return ` ${item.unitsSold} units • ₹${item.revenue.toLocaleString('en-IN')}`;
              }
            }
          }
        },
        scales: {
          x: { beginAtZero: true, ticks: { precision: 0 } }
        }
      }
    };
    this.topProductsChart = new Chart(canvas, config);
  }

  private renderCategoryChart(): void {
    const canvas = this.categoryCanvas()?.nativeElement;
    if (!canvas) return;
    const items = this.categoryBreakdown();
    if (!items.length) return;

    this.categoryChart?.destroy();
    const palette = ['#2874F0', '#fb641b', '#388e3c', '#7c4dff', '#e91e63', '#009688', '#FFB300', '#5d4037', '#455a64', '#c62828'];
    const config: ChartConfiguration<'doughnut'> = {
      type: 'doughnut',
      data: {
        labels: items.map(c => c.category),
        datasets: [{
          data: items.map(c => c.revenue),
          backgroundColor: items.map((_, i) => palette[i % palette.length]),
          borderWidth: 2,
          borderColor: '#fff'
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { position: 'right' },
          tooltip: {
            callbacks: {
              label: (ctx) => {
                const item = items[ctx.dataIndex];
                return ` ${item.category}: ₹${item.revenue.toLocaleString('en-IN')} (${item.unitsSold} units)`;
              }
            }
          }
        }
      }
    };
    this.categoryChart = new Chart(canvas, config);
  }

  private renderOrderStatusChart(): void {
    const canvas = this.orderStatusCanvas()?.nativeElement;
    if (!canvas) return;
    const items = this.orderStatusBreakdown();
    if (!items.length) return;

    this.orderStatusChart?.destroy();
    const colorMap: Record<string, string> = {
      Pending: '#FFB300',
      Processing: '#2874F0',
      Shipped: '#7c4dff',
      OutForDelivery: '#fb641b',
      Delivered: '#388e3c',
      Cancelled: '#c62828',
      Returned: '#5d4037'
    };
    const config: ChartConfiguration<'pie'> = {
      type: 'pie',
      data: {
        labels: items.map(s => s.status),
        datasets: [{
          data: items.map(s => s.count),
          backgroundColor: items.map(s => colorMap[s.status] ?? '#999'),
          borderWidth: 2,
          borderColor: '#fff'
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { position: 'right' },
          tooltip: {
            callbacks: {
              label: (ctx) => ` ${ctx.label}: ${ctx.parsed}`
            }
          }
        }
      }
    };
    this.orderStatusChart = new Chart(canvas, config);
  }

  private truncate(s: string, n: number): string {
    return s.length > n ? s.substring(0, n - 1) + '…' : s;
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
    this.createUserErrors = {};
    this.showCreateUserModal.set(true);
  }

  closeCreateUser(): void {
    this.showCreateUserModal.set(false);
    this.createUserErrors = {};
  }

  saveCreateUser(): void {
    const f = this.createUserForm;
    const errors: { [key: string]: string } = {};

    if (!f.email?.trim()) {
      errors['email'] = 'Email is required';
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(f.email)) {
      errors['email'] = 'Please enter a valid email address';
    }

    if (!f.username?.trim()) {
      errors['username'] = 'Username is required';
    } else if (f.username.length < 3) {
      errors['username'] = 'Username must be at least 3 characters';
    }

    if (!f.password) {
      errors['password'] = 'Password is required';
    } else if (f.password.length < 8) {
      errors['password'] = 'Password must be at least 8 characters';
    }

    if (!f.confirmPassword) {
      errors['confirmPassword'] = 'Please confirm the password';
    } else if (f.password !== f.confirmPassword) {
      errors['confirmPassword'] = 'Passwords do not match';
    }

    if (!f.role) {
      errors['role'] = 'Role is required';
    }

    this.createUserErrors = errors;
    if (Object.keys(errors).length > 0) {
      this.notificationService.showError('Please fix the highlighted fields');
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

  clearCreateUserError(field: string): void {
    if (this.createUserErrors[field]) {
      delete this.createUserErrors[field];
      this.createUserErrors = { ...this.createUserErrors };
    }
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

  unlockUser(user: User): void {
    this.http.post(`${this.apiUrl}/auth/users/${user.id}/unlock`, {}).subscribe({
      next: () => {
        this.notificationService.showSuccess(`User ${user.username} unlocked`);
        this.loadUsers();
      },
      error: (err) => this.notificationService.showError(err.error?.error || 'Failed to unlock user')
    });
  }

  openAddProduct(): void {
    this.editingProduct = null;
    this.productForm = { name: '', description: '', price: 0, originalPrice: 0, stock: 0, category: '', brand: '' };
    this.productImages.set([]);
    this.selectedFiles = [];
    this.imagePreviews.set([]);
    this.showProductModal.set(true);
  }

  openEditProduct(product: Product): void {
    this.editingProduct = product;
    this.productForm = { name: product.name, description: product.description, price: product.price, originalPrice: product.originalPrice || 0, stock: product.stock, category: product.category, brand: product.brand || '' };
    this.productImages.set(product.images || []);
    this.selectedFiles = [];
    this.imagePreviews.set([]);
    this.productVariants.set(product.variants || []);
    this.showProductModal.set(true);
  }

  closeProductModal(): void {
    this.showProductModal.set(false);
    this.editingProduct = null;
    this.productImages.set([]);
    this.selectedFiles = [];
    this.imagePreviews.set([]);
    this.productVariants.set([]);
  }

  onFilesSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files) {
      for (let i = 0; i < input.files.length; i++) {
        this.selectedFiles.push(input.files[i]);
        const reader = new FileReader();
        reader.onload = () => {
          this.imagePreviews.update(p => [...p, reader.result as string]);
        };
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
    this.http.delete(`${this.apiUrl}/products/images/${imageId}`).subscribe({
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

    const finish = () => {
      if (this.selectedFiles.length > 0 && this.editingProduct) {
        this.uploadMultipleImages(this.editingProduct.id);
      } else if (this.selectedFiles.length > 0 && this.createdProductId) {
        this.uploadMultipleImages(this.createdProductId);
      } else {
        this.notificationService.showSuccess(this.editingProduct ? 'Product updated' : 'Product added');
        this.closeProductModal();
        this.loadProducts();
        this.loadDashboard();
      }
    };

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
        next: (updated) => finish(),
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
          this.createdProductId = created.id;
          finish();
        },
        error: (err) => this.notificationService.showError(err.error?.error || 'Failed to add product')
      });
    }
  }

  private createdProductId: number | null = null;

  private uploadMultipleImages(productId: number): void {
    const total = this.selectedFiles.length;
    let completed = 0;
    for (const file of this.selectedFiles) {
      const formData = new FormData();
      formData.append('file', file);
      this.http.post(`${this.apiUrl}/products/${productId}/images`, formData).subscribe({
        next: () => {
          completed++;
          if (completed === total) {
            this.notificationService.showSuccess(this.editingProduct ? 'Product updated' : 'Product added');
            this.closeProductModal();
            this.loadProducts();
            this.loadDashboard();
          }
        },
        error: () => {
          completed++;
          this.notificationService.showError('Failed to upload some images');
          if (completed === total) {
            this.closeProductModal();
            this.loadProducts();
            this.loadDashboard();
          }
        }
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
  }

  // Variant management
  openAddVariant(): void {
    this.editingVariant = null;
    this.variantForm = { name: '', price: 0, stock: 0, sortOrder: this.productVariants().length };
    this.showVariantModal.set(true);
  }

  openEditVariant(v: ProductVariant): void {
    this.editingVariant = v;
    this.variantForm = { name: v.name, price: v.price, stock: v.stock, sortOrder: v.sortOrder };
    this.showVariantModal.set(true);
  }

  closeVariantModal(): void {
    this.showVariantModal.set(false);
    this.editingVariant = null;
  }

  saveVariant(): void {
    const product = this.editingProduct;
    if (!product || !this.variantForm.name || this.variantForm.price <= 0) return;

    if (this.editingVariant) {
      this.http.put<ProductVariant>(`${this.apiUrl}/products/${product.id}/variants/${this.editingVariant.id}`, this.variantForm).subscribe({
        next: () => {
          this.notificationService.showSuccess('Variant updated');
          this.closeVariantModal();
          this.loadProductVariants(product.id);
        },
        error: () => this.notificationService.showError('Failed to update variant')
      });
    } else {
      this.http.post<ProductVariant>(`${this.apiUrl}/products/${product.id}/variants`, this.variantForm).subscribe({
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
    const product = this.editingProduct;
    if (!product || !confirm('Delete this variant?')) return;
    this.http.delete(`${this.apiUrl}/products/${product.id}/variants/${v.id}`).subscribe({
      next: () => {
        this.notificationService.showSuccess('Variant deleted');
        this.productVariants.update(list => list.filter(x => x.id !== v.id));
      },
      error: () => this.notificationService.showError('Failed to delete variant')
    });
  }

  private loadProductVariants(productId: number): void {
    this.http.get<ProductVariant[]>(`${this.apiUrl}/products/${productId}/variants`).subscribe({
      next: (variants) => this.productVariants.set(variants),
      error: () => this.notificationService.showError('Failed to load variants')
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

  // Return Management
  loadReturns(): void {
    this.isLoading.set(true);
    this.returnService.getAll(this.returnPage(), 20, this.returnStatusFilter || undefined).subscribe({
      next: (res) => {
        this.returns.set(res.items);
        this.totalReturns.set(res.totalCount);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  getReturnStatusClass(status: string): string {
    const map: Record<string, string> = {
      Requested: 'status-pending',
      Approved: 'status-processing',
      Rejected: 'status-cancelled',
      RefundInitiated: 'status-shipped',
      Refunded: 'status-delivered'
    };
    return map[status] || '';
  }

  openReturnDetail(r: ReturnRequest): void {
    this.selectedReturn.set(r);
    this.returnAdminNote.set(r.adminNote || '');
    this.returnDetailAction.set('');
    this.showReturnDetailModal.set(true);
  }

  closeReturnDetail(): void {
    this.showReturnDetailModal.set(false);
    this.selectedReturn.set(null);
    this.returnAdminNote.set('');
  }

  approveReturn(): void {
    const r = this.selectedReturn();
    if (!r) return;
    this.returnService.updateStatus(r.id, 'Approved', this.returnAdminNote() || undefined).subscribe({
      next: () => {
        this.notificationService.showSuccess('Return request approved');
        this.closeReturnDetail();
        this.loadReturns();
      },
      error: (err) => this.notificationService.showError(err.error?.error || 'Failed to approve return')
    });
  }

  rejectReturn(): void {
    const r = this.selectedReturn();
    if (!r) return;
    this.returnService.updateStatus(r.id, 'Rejected', this.returnAdminNote() || undefined).subscribe({
      next: () => {
        this.notificationService.showSuccess('Return request rejected');
        this.closeReturnDetail();
        this.loadReturns();
      },
      error: (err) => this.notificationService.showError(err.error?.error || 'Failed to reject return')
    });
  }

  initiateRefund(): void {
    const r = this.selectedReturn();
    if (!r) return;
    this.returnService.updateStatus(r.id, 'RefundInitiated', this.returnAdminNote() || undefined).subscribe({
      next: () => {
        this.notificationService.showSuccess('Refund initiated');
        this.closeReturnDetail();
        this.loadReturns();
      },
      error: (err) => this.notificationService.showError(err.error?.error || 'Failed to initiate refund')
    });
  }

  markRefunded(): void {
    const r = this.selectedReturn();
    if (!r) return;
    this.returnService.updateStatus(r.id, 'Refunded', this.returnAdminNote() || undefined).subscribe({
      next: () => {
        this.notificationService.showSuccess('Refund completed');
        this.closeReturnDetail();
        this.loadReturns();
      },
      error: (err) => this.notificationService.showError(err.error?.error || 'Failed to mark refunded')
    });
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
