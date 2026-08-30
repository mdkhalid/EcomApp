import { Component, inject, signal, OnInit, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ReturnService } from '../../../services/return.service';
import { NotificationService } from '../../../services/notification.service';
import { ReturnRequest } from '../../../models/return.model';
import { AdminTableComponent, ColumnDef, ActionDef } from '../shared/admin-table.component';
import { AdminModalComponent } from '../shared/admin-modal.component';

@Component({
  selector: 'app-admin-returns',
  standalone: true,
  imports: [CommonModule, FormsModule, AdminTableComponent, AdminModalComponent],
  template: `
    <div class="admin-returns">
      <header class="section-header">
        <h2>Returns</h2>
      </header>

      <div class="filters">
        <select [(ngModel)]="statusFilter" (ngModelChange)="loadReturns()" class="filter-select">
          <option value="">All Statuses</option>
          <option value="Requested">Requested</option>
          <option value="Approved">Approved</option>
          <option value="Rejected">Rejected</option>
          <option value="RefundInitiated">Refund Initiated</option>
          <option value="Refunded">Refunded</option>
        </select>
        <div class="pagination-controls" *ngIf="pagination">
          <button class="btn btn-sm" (click)="prevPage()" [disabled]="currentPage <= 1">Previous</button>
          <span>Page {{ currentPage }} of {{ totalPages }}</span>
          <button class="btn btn-sm" (click)="nextPage()" [disabled]="currentPage >= totalPages">Next</button>
        </div>
      </div>

      <app-admin-table
        [data]="returns()"
        [columns]="returnColumns"
        [actions]="returnActions"
        [emptyMessage]="'No returns found'"
      />

      @if (showReturnDetailModal()) {
        <app-admin-modal
          [isOpen]="showReturnDetailModal()"
          [title]="'Return # ' + (selectedReturn()?.id || '')"
          (close)="closeReturnDetail()">
          <div class="return-detail">
            <div class="detail-row">
              <span class="label">Order ID:</span>
              <span class="value">#{{ selectedReturn()?.orderId }}</span>
            </div>
            <div class="detail-row">
              <span class="label">Product:</span>
              <span class="value">{{ selectedReturn()?.productName }}</span>
            </div>
            <div class="detail-row">
              <span class="label">Reason:</span>
              <span class="value">{{ selectedReturn()?.reason }}</span>
            </div>
            <div class="detail-row">
              <span class="label">Quantity:</span>
              <span class="value">{{ selectedReturn()?.quantity }}</span>
            </div>
            <div class="detail-row">
              <span class="label">Status:</span>
              <span class="value status-badge" [class]="getStatusClass(selectedReturn()?.status || '')">{{ selectedReturn()?.status }}</span>
            </div>
            <div class="detail-row">
              <span class="label">Requested:</span>
              <span class="value">{{ selectedReturn()?.requestedAt | date:'medium' }}</span>
            </div>
            <div class="detail-row">
              <span class="label">Admin Note:</span>
              <textarea [(ngModel)]="returnAdminNote" rows="3" class="admin-note" placeholder="Add admin note..."></textarea>
            </div>
            <div class="action-buttons">
              <button class="btn btn-success" (click)="approveReturn()" [disabled]="!canApprove()">Approve</button>
              <button class="btn btn-danger" (click)="rejectReturn()" [disabled]="!canReject()">Reject</button>
              <button class="btn btn-warning" (click)="initiateRefund()" [disabled]="!canRefund()">Initiate Refund</button>
              <button class="btn btn-success" (click)="markRefunded()" [disabled]="!canMarkRefunded()">Mark Refunded</button>
            </div>
          </div>
        </app-admin-modal>
      }
    </div>
  `,
  styles: [`
    .admin-returns { padding: 1.5rem; }
    .section-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1.5rem; }
    .section-header h2 { margin: 0; font-size: 1.5rem; font-weight: 600; }
    .filters { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1.5rem; flex-wrap: wrap; gap: 1rem; }
    .filter-select { padding: 0.5rem; border: 1px solid var(--border-color, #ddd); border-radius: 6px; background: white; }
    .pagination-controls { display: flex; align-items: center; gap: 0.75rem; }
    .btn { padding: 0.5rem 1rem; border: none; border-radius: 6px; font-weight: 500; cursor: pointer; }
    .btn-sm { padding: 0.375rem 0.75rem; font-size: 0.875rem; }
    .btn:disabled { opacity: 0.5; cursor: not-allowed; }
    .btn-success { background: var(--success, #388e3c); color: white; }
    .btn-danger { background: var(--danger, #dc3545); color: white; }
    .btn-warning { background: var(--warning, #ffb300); color: #333; }
    .return-detail { display: flex; flex-direction: column; gap: 1rem; }
    .detail-row { display: flex; flex-direction: column; gap: 0.25rem; }
    .detail-row .label { font-weight: 500; color: var(--on-surface-variant, #666); font-size: 0.875rem; }
    .detail-row .value { font-size: 1rem; }
    .status-badge { display: inline-block; padding: 0.25rem 0.75rem; border-radius: 20px; font-size: 0.875rem; font-weight: 500; }
    .status-badge.status-pending { background: #fff3e0; color: #e65100; }
    .status-badge.status-processing { background: #e3f2fd; color: #1565c0; }
    .status-badge.status-cancelled { background: #fce4ec; color: #c62828; }
    .status-badge.status-delivered { background: #e8f5e9; color: #2e7d32; }
    .status-badge.status-shipped { background: #f3e5f5; color: #6a1b9a; }
    .admin-note { width: 100%; padding: 0.5rem; border: 1px solid var(--border-color, #ddd); border-radius: 6px; font-family: inherit; }
    .action-buttons { display: flex; gap: 0.75rem; flex-wrap: wrap; margin-top: 0.5rem; }
  `]
})
export class AdminReturnsComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly returnService = inject(ReturnService);
  private readonly notificationService = inject(NotificationService);

  returns = signal<ReturnRequest[]>([]);
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

  showReturnDetailModal = signal(false);
  selectedReturn = signal<ReturnRequest | null>(null);
  returnAdminNote = '';

  returnColumns: ColumnDef<ReturnRequest>[] = [
    { key: 'id', header: 'ID', render: (r) => `#${r.id}` },
    { key: 'orderId', header: 'Order', render: (r) => `#${r.orderId}` },
    { key: 'productName', header: 'Product' },
    { key: 'quantity', header: 'Qty' },
    { key: 'reason', header: 'Reason' },
    { key: 'status', header: 'Status', render: (r) => r.status },
    { key: 'requestedAt', header: 'Requested', render: (r) => r.requestedAt ? new Date(r.requestedAt).toLocaleDateString() : new Date(r.createdAt).toLocaleDateString() }
  ];

  returnActions: ActionDef<ReturnRequest>[] = [
    { label: 'View', action: (r) => this.openReturnDetail(r), class: 'primary' }
  ];

  ngOnInit(): void {
    this.loadReturns();
  }

  loadReturns(): void {
    this.returnService.getAll(this.currentPage, this.pageSize, this.statusFilter || undefined).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (res) => {
        this.returns.set(res.items);
        this.totalItems = res.totalCount;
      },
      error: () => this.notificationService.showError('Failed to load returns')
    });
  }

  prevPage(): void {
    if (this.currentPage > 1) {
      this.currentPage--;
      this.loadReturns();
    }
  }

  nextPage(): void {
    if (this.currentPage < this.totalPages) {
      this.currentPage++;
      this.loadReturns();
    }
  }

  openReturnDetail(r: ReturnRequest): void {
    this.selectedReturn.set(r);
    this.returnAdminNote = r.adminNote || '';
    this.showReturnDetailModal.set(true);
  }

  closeReturnDetail(): void {
    this.showReturnDetailModal.set(false);
    this.selectedReturn.set(null);
    this.returnAdminNote = '';
  }

  canApprove(): boolean {
    return this.selectedReturn()?.status === 'Requested';
  }

  canReject(): boolean {
    return this.selectedReturn()?.status === 'Requested';
  }

  canRefund(): boolean {
    return this.selectedReturn()?.status === 'Approved';
  }

  canMarkRefunded(): boolean {
    return this.selectedReturn()?.status === 'RefundInitiated';
  }

  getStatusClass(status: string): string {
    const map: Record<string, string> = {
      Requested: 'status-pending',
      Approved: 'status-processing',
      Rejected: 'status-cancelled',
      RefundInitiated: 'status-shipped',
      Refunded: 'status-delivered'
    };
    return map[status] || '';
  }

  approveReturn(): void {
    const r = this.selectedReturn();
    if (!r) return;
    this.returnService.updateStatus(r.id, 'Approved', this.returnAdminNote || undefined).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
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
    this.returnService.updateStatus(r.id, 'Rejected', this.returnAdminNote || undefined).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
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
    this.returnService.updateStatus(r.id, 'RefundInitiated', this.returnAdminNote || undefined).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
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
    this.returnService.updateStatus(r.id, 'Refunded', this.returnAdminNote || undefined).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.notificationService.showSuccess('Refund completed');
        this.closeReturnDetail();
        this.loadReturns();
      },
      error: (err) => this.notificationService.showError(err.error?.error || 'Failed to mark refunded')
    });
  }
}