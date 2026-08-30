import { Component, inject, OnInit, signal, DestroyRef, computed } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Chart, registerables } from 'chart.js';
import { ProductService } from '../../../services/product.service';
import { OrderService } from '../../../services/order.service';
import { AuthService } from '../../../services/auth.service';
import { AnalyticsService } from '../../../services/analytics.service';
import { NotificationService } from '../../../services/notification.service';
import { Product } from '../../../models/product.model';
import { Order } from '../../../models/order.model';
import { AnalyticsOverview, RevenueSummary, TopProduct, CategoryBreakdown, OrderStatusBreakdown } from '../../../models/analytics.model';
import { AdminTableComponent, ColumnDef, ActionDef } from '../shared/admin-table.component';

Chart.register(...registerables);

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, AdminTableComponent],
  template: `
    <div class="admin-dashboard">
      <header class="dashboard-header">
        <h1>Dashboard</h1>
        <p class="subtitle">Overview of your store performance</p>
      </header>

      <div class="stats-grid">
        <div class="stat-card">
          <div class="stat-icon products">📦</div>
          <div class="stat-info">
            <span class="stat-value">{{ stats().totalProducts }}</span>
            <span class="stat-label">Total Products</span>
          </div>
        </div>
        <div class="stat-card">
          <div class="stat-icon orders">📋</div>
          <div class="stat-info">
            <span class="stat-value">{{ stats().totalOrders }}</span>
            <span class="stat-label">Total Orders</span>
          </div>
        </div>
        <div class="stat-card">
          <div class="stat-icon users">👥</div>
          <div class="stat-info">
            <span class="stat-value">{{ stats().totalUsers }}</span>
            <span class="stat-label">Total Users</span>
          </div>
        </div>
        <div class="stat-card">
          <div class="stat-icon revenue">💰</div>
          <div class="stat-info">
            <span class="stat-value">₹{{ stats().totalRevenue | number }}</span>
            <span class="stat-label">Total Revenue</span>
          </div>
        </div>
      </div>

      <div class="charts-grid">
        <div class="chart-card">
          <h3>Revenue Overview</h3>
          <div class="chart-actions">
            <select [(ngModel)]="revenuePeriod" (change)="loadRevenue()">
              <option value="daily">Daily</option>
              <option value="weekly">Weekly</option>
              <option value="monthly">Monthly</option>
            </select>
          </div>
          <canvas #revenueChart></canvas>
        </div>

        <div class="chart-card">
          <h3>Order Status</h3>
          <canvas #orderStatusChart></canvas>
        </div>

        <div class="chart-card">
          <h3>Top Products</h3>
          <canvas #topProductsChart></canvas>
        </div>

        <div class="chart-card">
          <h3>Category Breakdown</h3>
          <canvas #categoryChart></canvas>
        </div>
      </div>

      <div class="recent-orders">
        <h3>Recent Orders</h3>
        <app-admin-table
          [data]="recentOrders()"
          [columns]="orderColumns"
          [actions]="orderActions"
        />
      </div>
    </div>
  `,
  styles: [`
    .admin-dashboard {
      padding: 1.5rem;
    }
    .dashboard-header {
      margin-bottom: 1.5rem;
    }
    .dashboard-header h1 {
      margin: 0 0 0.5rem;
      font-size: 1.75rem;
      font-weight: 600;
    }
    .subtitle {
      color: var(--on-surface-variant, #666);
      margin: 0;
    }
    .stats-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
      gap: 1rem;
      margin-bottom: 2rem;
    }
    .stat-card {
      background: var(--surface, white);
      border-radius: 12px;
      padding: 1.5rem;
      display: flex;
      align-items: center;
      gap: 1rem;
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
      transition: transform 0.2s, box-shadow 0.2s;
    }
    .stat-card:hover {
      transform: translateY(-2px);
      box-shadow: 0 8px 24px rgba(0, 0, 0, 0.12);
    }
    .stat-icon {
      width: 48px;
      height: 48px;
      border-radius: 12px;
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 1.5rem;
    }
    .stat-icon.products { background: rgba(40, 116, 240, 0.1); color: #2874F0; }
    .stat-icon.orders { background: rgba(251, 100, 27, 0.1); color: #fb641b; }
    .stat-icon.users { background: rgba(56, 142, 60, 0.1); color: #388e3c; }
    .stat-icon.revenue { background: rgba(124, 77, 255, 0.1); color: #7c4dff; }
    .stat-value {
      font-size: 1.75rem;
      font-weight: 700;
      line-height: 1.2;
    }
    .stat-label {
      color: var(--on-surface-variant, #666);
      font-size: 0.875rem;
    }
    .charts-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(400px, 1fr));
      gap: 1.5rem;
      margin-bottom: 2rem;
    }
    .chart-card {
      background: var(--surface, white);
      border-radius: 12px;
      padding: 1.5rem;
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
    }
    .chart-card h3 {
      margin: 0 0 1rem;
      font-size: 1rem;
      font-weight: 600;
    }
    .chart-actions {
      margin-bottom: 1rem;
    }
    .chart-actions select {
      padding: 0.375rem 0.75rem;
      border: 1px solid var(--border-color, #ddd);
      border-radius: 4px;
      background: var(--surface, white);
    }
    .chart-card canvas {
      max-height: 300px;
    }
    .recent-orders {
      background: var(--surface, white);
      border-radius: 12px;
      padding: 1.5rem;
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
    }
    .recent-orders h3 {
      margin: 0 0 1rem;
      font-size: 1rem;
      font-weight: 600;
    }
  `]
})
export class AdminDashboardComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly productService = inject(ProductService);
  private readonly orderService = inject(OrderService);
  private readonly authService = inject(AuthService);
  private readonly analyticsService = inject(AnalyticsService);
  private readonly notificationService = inject(NotificationService);

  stats = signal({ totalProducts: 0, totalOrders: 0, totalUsers: 0, totalRevenue: 0 });
  recentOrders = signal<Order[]>([]);
  revenuePeriod = signal<'daily' | 'weekly' | 'monthly'>('monthly');
  analyticsLoading = signal(false);

  private revenueChart?: Chart;
  private orderStatusChart?: Chart;
  private topProductsChart?: Chart;
  private categoryChart?: Chart;

  orderColumns: ColumnDef<Order>[] = [
    { key: 'id', header: 'Order ID', render: (o) => `#${o.id}` },
    { key: 'customerEmail', header: 'User', render: (o) => o.customerEmail || 'Guest' },
    { key: 'totalAmount', header: 'Amount', render: (o) => `₹${o.totalAmount.toLocaleString('en-IN')}` },
    { key: 'status', header: 'Status', render: (o) => o.status },
    { key: 'createdAt', header: 'Date', render: (o) => new Date(o.createdAt).toLocaleDateString() }
  ];

  orderActions: ActionDef<Order>[] = [
    { label: 'View', action: (o) => this.viewOrder(o), class: 'primary' }
  ];

  ngOnInit(): void {
    this.loadDashboard();
  }

  loadDashboard(): void {
    this.analyticsLoading.set(true);

    this.productService.search({ pageNumber: 1, pageSize: 1000 }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (res) => {
        this.stats.update(s => ({ ...s, totalProducts: res.items.length }));
      }
    });

    this.orderService.getOrders().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (orders) => {
        this.stats.update(s => ({
          ...s,
          totalOrders: orders.length,
          totalRevenue: orders.reduce((sum, o) => sum + o.totalAmount, 0)
        }));
        this.recentOrders.set(orders.slice(0, 10));
      }
    });

    this.loadUsersCount();

    this.loadAnalytics();
  }

  private loadUsersCount(): void {
    // Fallback if getUsers doesn't exist
    this.analyticsService.getOverview().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        this.stats.update(s => ({ ...s, totalUsers: data.totalUsers || 0 }));
      }
    });
  }

  loadAnalytics(): void {
    this.analyticsLoading.set(true);
    this.analyticsService.getOverview().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        this.renderOrderStatusChart(data.orderStatusBreakdown);
        if (data.topProducts?.length) {
          this.renderTopProductsChart(data.topProducts);
        }
        this.analyticsLoading.set(false);
      },
      error: () => {
        this.analyticsLoading.set(false);
        this.notificationService.showError('Failed to load analytics');
      }
    });
    this.analyticsService.getCategoryBreakdown().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => this.renderCategoryChart(data),
      error: () => this.notificationService.showError('Failed to load category breakdown')
    });
    this.loadRevenue();
  }

  loadRevenue(): void {
    this.analyticsService.getRevenue(this.revenuePeriod()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => this.renderRevenueChart(data),
      error: () => this.notificationService.showError('Failed to load revenue')
    });
  }

  private renderRevenueChart(data: RevenueSummary): void {
    const canvas = document.querySelector('#revenueChart') as HTMLCanvasElement;
    if (!canvas) return;

    this.revenueChart?.destroy();
    this.revenueChart = new Chart(canvas, {
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
          borderWidth: 2
        }, {
          label: 'Orders',
          data: data.points.map(p => p.orderCount),
          borderColor: '#fb641b',
          backgroundColor: 'transparent',
          tension: 0.35,
          pointRadius: 3,
          borderWidth: 2,
          borderDash: [5, 5],
          yAxisID: 'y1'
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        interaction: { mode: 'index', intersect: false },
        plugins: {
          legend: { position: 'top' },
        },
        scales: {
          y: { beginAtZero: true },
          y1: { beginAtZero: true, position: 'right', grid: { drawOnChartArea: false } }
        }
      }
    });
  }

  private renderOrderStatusChart(items: OrderStatusBreakdown[]): void {
    const canvas = document.querySelector('#orderStatusChart') as HTMLCanvasElement;
    if (!canvas || !items.length) return;

    this.orderStatusChart?.destroy();
    const colorMap: Record<string, string> = {
      Pending: '#FFB300', Processing: '#2874F0', Shipped: '#7c4dff',
      OutForDelivery: '#fb641b', Delivered: '#388e3c', Cancelled: '#c62828', Returned: '#5d4037'
    };

    this.orderStatusChart = new Chart(canvas, {
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
        plugins: { legend: { position: 'right' } }
      }
    });
  }

  private renderTopProductsChart(items: TopProduct[]): void {
    const canvas = document.querySelector('#topProductsChart') as HTMLCanvasElement;
    if (!canvas || !items.length) return;

    this.topProductsChart?.destroy();
    this.topProductsChart = new Chart(canvas, {
      type: 'bar',
      data: {
        labels: items.map(p => p.productName.length > 20 ? p.productName.substring(0, 20) + '…' : p.productName),
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
        plugins: { legend: { display: false } },
        scales: { x: { beginAtZero: true } }
      }
    });
  }

  private renderCategoryChart(items: CategoryBreakdown[]): void {
    const canvas = document.querySelector('#categoryChart') as HTMLCanvasElement;
    if (!canvas || !items.length) return;

    this.categoryChart?.destroy();
    const palette = ['#2874F0', '#fb641b', '#388e3c', '#7c4dff', '#e91e63', '#009688', '#FFB300', '#5d4037'];

    this.categoryChart = new Chart(canvas, {
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
        plugins: { legend: { position: 'right' } }
      }
    });
  }

  viewOrder(order: Order): void {
    // Navigate to order detail
  }
}