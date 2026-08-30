import { Component, inject, signal, OnInit, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Chart, registerables } from 'chart.js';
import { AnalyticsService } from '../../../services/analytics.service';
import { NotificationService } from '../../../services/notification.service';
import { AuthService } from '../../../services/auth.service';
import { AnalyticsOverview, RevenueSummary, TopProduct, CategoryBreakdown, OrderStatusBreakdown, PageViewSummary, TopPage, TopSearch } from '../../../models/analytics.model';

Chart.register(...registerables);

@Component({
  selector: 'app-admin-analytics',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="admin-analytics">
      <header class="section-header">
        <h2>Analytics</h2>
      </header>

      <div class="charts-grid">
        <div class="chart-card">
          <div class="chart-header">
            <h3>Revenue</h3>
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

      <div class="charts-grid visitor-grid">
        <div class="chart-card full-width">
          <div class="chart-header">
            <h3>Page Views</h3>
            <select [(ngModel)]="visitorPeriod" (change)="loadVisitorAnalytics()">
              <option value="1d">24 Hours</option>
              <option value="7d">7 Days</option>
              <option value="30d">30 Days</option>
            </select>
          </div>
          <canvas #pageViewsChart></canvas>
        </div>

        <div class="chart-card">
          <h3>Top Pages</h3>
          <div class="top-list">
            @for (page of topPages(); track page.path) {
              <div class="top-item">
                <span class="top-label">{{ page.path }}</span>
                <span class="top-value">{{ page.count }} views</span>
              </div>
            }
          </div>
        </div>

        <div class="chart-card">
          <h3>Top Searches</h3>
          <div class="top-list">
            @for (search of topSearches(); track search.keyword) {
              <div class="top-item">
                <span class="top-label">{{ search.keyword }}</span>
                <span class="top-value">{{ search.count }} searches</span>
              </div>
            }
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .admin-analytics { padding: 1.5rem; }
    .section-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1.5rem; }
    .section-header h2 { margin: 0; font-size: 1.5rem; font-weight: 600; }
    .charts-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(400px, 1fr));
      gap: 1.5rem;
      margin-bottom: 2rem;
    }
    .visitor-grid { grid-template-columns: repeat(auto-fit, minmax(300px, 1fr)); }
    .full-width { grid-column: 1 / -1; }
    .chart-card {
      background: var(--surface, white);
      border-radius: 12px;
      padding: 1.5rem;
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
    }
    .chart-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem; }
    .chart-header h3 { margin: 0; font-size: 1rem; font-weight: 600; }
    .chart-header select { padding: 0.375rem 0.75rem; border: 1px solid var(--border-color, #ddd); border-radius: 4px; background: white; }
    .chart-card canvas { max-height: 300px; }
    .top-list { display: flex; flex-direction: column; gap: 0.5rem; }
    .top-item { display: flex; justify-content: space-between; align-items: center; padding: 0.75rem; background: var(--surface-variant, #fafafa); border-radius: 6px; }
    .top-label { flex: 1; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
    .top-value { color: var(--on-surface-variant, #666); font-size: 0.875rem; }
  `]
})
export class AdminAnalyticsComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly analyticsService = inject(AnalyticsService);
  private readonly notificationService = inject(NotificationService);
  private readonly authService = inject(AuthService);

  revenuePeriod = signal<'daily' | 'weekly' | 'monthly'>('monthly');
  visitorPeriod = signal('7d');
  analyticsLoading = signal(false);

  topProducts = signal<TopProduct[]>([]);
  categoryBreakdown = signal<CategoryBreakdown[]>([]);
  orderStatusBreakdown = signal<OrderStatusBreakdown[]>([]);
  pageViews = signal<PageViewSummary | null>(null);
  topPages = signal<TopPage[]>([]);
  topSearches = signal<TopSearch[]>([]);

  private revenueChart?: Chart;
  private orderStatusChart?: Chart;
  private topProductsChart?: Chart;
  private categoryChart?: Chart;
  private pageViewsChart?: Chart;

  ngOnInit(): void {
    this.loadAnalytics();
  }

  loadAnalytics(): void {
    this.analyticsLoading.set(true);
    this.analyticsService.getOverview().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        this.orderStatusBreakdown.set(data.orderStatusBreakdown);
        if (data.topProducts?.length) {
          this.topProducts.set(data.topProducts);
        }
        this.analyticsLoading.set(false);
        this.renderAllCharts();
        if (this.authService.isSuperAdmin()) {
          this.loadRevenue();
        }
      },
      error: () => {
        this.analyticsLoading.set(false);
        this.notificationService.showError('Failed to load analytics');
      }
    });
    this.analyticsService.getCategoryBreakdown().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => this.categoryBreakdown.set(data),
      error: () => this.notificationService.showError('Failed to load category breakdown')
    });
    this.loadVisitorAnalytics();
  }

  loadRevenue(): void {
    this.analyticsService.getRevenue(this.revenuePeriod()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => this.renderRevenueChart(data),
      error: () => this.notificationService.showError('Failed to load revenue')
    });
  }

  loadVisitorAnalytics(): void {
    this.analyticsService.getPageViews(this.visitorPeriod()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        this.pageViews.set(data);
        this.renderPageViewsChart();
      },
      error: () => this.notificationService.showError('Failed to load page views')
    });
    this.analyticsService.getTopPages(this.visitorPeriod()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => this.topPages.set(data),
      error: () => this.notificationService.showError('Failed to load top pages')
    });
    this.analyticsService.getTopSearches(this.visitorPeriod()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => this.topSearches.set(data),
      error: () => this.notificationService.showError('Failed to load top searches')
    });
  }

  private renderAllCharts(): void {
    this.renderOrderStatusChart();
    this.renderTopProductsChart();
    this.renderCategoryChart();
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
        plugins: { legend: { position: 'top' } },
        scales: { y: { beginAtZero: true }, y1: { beginAtZero: true, position: 'right', grid: { drawOnChartArea: false } } }
      }
    });
  }

  private renderOrderStatusChart(): void {
    const canvas = document.querySelector('#orderStatusChart') as HTMLCanvasElement;
    if (!canvas) return;
    const items = this.orderStatusBreakdown();
    if (!items.length) return;

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
      options: { responsive: true, maintainAspectRatio: false, plugins: { legend: { position: 'right' } } }
    });
  }

  private renderTopProductsChart(): void {
    const canvas = document.querySelector('#topProductsChart') as HTMLCanvasElement;
    if (!canvas) return;
    const items = this.topProducts();
    if (!items.length) return;

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
      options: { responsive: true, maintainAspectRatio: false, indexAxis: 'y', plugins: { legend: { display: false } }, scales: { x: { beginAtZero: true } } }
    });
  }

  private renderCategoryChart(): void {
    const canvas = document.querySelector('#categoryChart') as HTMLCanvasElement;
    if (!canvas) return;
    const items = this.categoryBreakdown();
    if (!items.length) return;

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
      options: { responsive: true, maintainAspectRatio: false, plugins: { legend: { position: 'right' } } }
    });
  }

  private renderPageViewsChart(): void {
    const canvas = document.querySelector('#pageViewsChart') as HTMLCanvasElement;
    if (!canvas) return;
    const data = this.pageViews();
    if (!data) return;

    this.pageViewsChart?.destroy();
    this.pageViewsChart = new Chart(canvas, {
      type: 'line',
      data: {
        labels: data.views.map(p => p.label),
        datasets: [{
          label: 'Page Views',
          data: data.views.map(p => p.count),
          borderColor: '#2874F0',
          backgroundColor: 'rgba(40, 116, 240, 0.12)',
          fill: true,
          tension: 0.35,
          pointRadius: 3,
          borderWidth: 2
        }, {
          label: 'Unique Visitors',
          data: data.uniqueVisitorsPoints.map(p => p.count),
          borderColor: '#fb641b',
          backgroundColor: 'rgba(251, 100, 27, 0.08)',
          fill: true,
          tension: 0.35,
          pointRadius: 3,
          borderWidth: 2,
          borderDash: [5, 5]
        }]
      },
      options: { responsive: true, maintainAspectRatio: false, interaction: { mode: 'index', intersect: false }, plugins: { legend: { position: 'top' } }, scales: { y: { beginAtZero: true } } }
    });
  }
}