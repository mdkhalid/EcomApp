import { Component, inject, signal, OnInit, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { OrderService } from '../../../services/order.service';
import { NotificationService } from '../../../services/notification.service';
import { Order } from '../../../models/order.model';
import { API_URL } from '../../../utils/api-config';
import { AdminTableComponent, ColumnDef, ActionDef } from '../shared/admin-table.component';

@Component({
  selector: 'app-admin-orders',
  standalone: true,
  imports: [CommonModule, FormsModule, AdminTableComponent],
  template: `
    <div class="admin-orders">
      <header class="section-header">
        <h2>Orders</h2>
      </header>

      <div class="filters">
        <select [(ngModel)]="statusFilter" (ngModelChange)="loadOrders()" class="filter-select">
          <option value="">All Statuses</option>
          <option value="Pending">Pending</option>
          <option value="Processing">Processing</option>
          <option value="Shipped">Shipped</option>
          <option value="OutForDelivery">Out for Delivery</option>
          <option value="Delivered">Delivered</option>
          <option value="Cancelled">Cancelled</option>
          <option value="Returned">Returned</option>
        </select>
        <div class="pagination-controls" *ngIf="pagination">
          <button class="btn btn-sm" (click)="prevPage()" [disabled]="currentPage <= 1">Previous</button>
          <span>Page {{ currentPage }} of {{ totalPages }}</span>
          <button class="btn btn-sm" (click)="nextPage()" [disabled]="currentPage >= totalPages">Next</button>
        </div>
      </div>

      <app-admin-table
        [data]="orders()"
        [columns]="orderColumns"
        [actions]="orderActions"
        [emptyMessage]="'No orders found'"
      />
    </div>
  `,
  styles: [`
    .admin-orders { padding: 1.5rem; }
    .section-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1.5rem; }
    .section-header h2 { margin: 0; font-size: 1.5rem; font-weight: 600; }
    .filters { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1.5rem; flex-wrap: wrap; gap: 1rem; }
    .filter-select { padding: 0.5rem; border: 1px solid var(--border-color, #ddd); border-radius: 6px; background: white; }
    .pagination-controls { display: flex; align-items: center; gap: 0.75rem; }
    .btn { padding: 0.5rem 1rem; border: none; border-radius: 6px; font-weight: 500; cursor: pointer; }
    .btn-sm { padding: 0.375rem 0.75rem; font-size: 0.875rem; }
    .btn:disabled { opacity: 0.5; cursor: not-allowed; }
  `]
})
export class AdminOrdersComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly orderService = inject(OrderService);
  private readonly notificationService = inject(NotificationService);
  private readonly http = inject(HttpClient);
  private readonly apiUrl = API_URL;

  orders = signal<Order[]>([]);
  statusFilter = '';
  currentPage = 1;
  pageSize = 20;
  totalItems = 0;

  get totalPages(): number {
    return Math.ceil(this.totalItems / this.pageSize) || 1;
  }

  get pagination() {
    return { current: this.currentPage, total: this.totalPages };
  }

  orderColumns: ColumnDef<Order>[] = [
    { key: 'id', header: 'Order ID', render: (o) => `#${o.id}` },
    { key: 'customerEmail', header: 'User', render: (o) => o.customerEmail || 'Guest' },
    { key: 'totalAmount', header: 'Amount', render: (o) => `₹${o.totalAmount.toLocaleString('en-IN')}` },
    { key: 'status', header: 'Status', render: (o) => o.status },
    { key: 'createdAt', header: 'Date', render: (o) => new Date(o.createdAt).toLocaleDateString() }
  ];

  orderActions: ActionDef<Order>[] = [
    { label: 'View', action: (o) => this.viewOrder(o), class: 'primary' },
    { label: 'Update Status', action: (o) => this.updateStatus(o), class: 'secondary' }
  ];

  ngOnInit(): void {
    this.loadOrders();
  }

  loadOrders(): void {
    let url = `${this.apiUrl}/orders?pageNumber=${this.currentPage}&pageSize=${this.pageSize}`;
    if (this.statusFilter) url += `&status=${this.statusFilter}`;

    this.http.get<{ items: Order[]; totalCount: number }>(url).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (res) => {
        this.orders.set(res.items);
        this.totalItems = res.totalCount;
      },
      error: () => this.notificationService.showError('Failed to load orders')
    });
  }

  prevPage(): void {
    if (this.currentPage > 1) {
      this.currentPage--;
      this.loadOrders();
    }
  }

  nextPage(): void {
    if (this.currentPage < this.totalPages) {
      this.currentPage++;
      this.loadOrders();
    }
  }

  viewOrder(order: Order): void {
    // Navigate to order detail
  }

  updateStatus(order: Order): void {
    const statuses = ['Pending', 'Processing', 'Shipped', 'OutForDelivery', 'Delivered', 'Cancelled'];
    const currentIndex = statuses.indexOf(order.status);
    const nextStatus = statuses[(currentIndex + 1) % statuses.length];

    if (confirm(`Change order #${order.id} status to ${nextStatus}?`)) {
      this.orderService.updateStatus(order.id, nextStatus).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: () => {
          this.notificationService.showSuccess('Order status updated');
          this.loadOrders();
        },
        error: (err) => this.notificationService.showError(err.error?.error || 'Failed to update status')
      });
    }
  }
}