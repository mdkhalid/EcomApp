import { Component, inject, signal, OnInit, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CouponService } from '../../../services/coupon.service';
import { NotificationService } from '../../../services/notification.service';
import { Coupon, CreateCoupon } from '../../../models/coupon.model';
import { AdminTableComponent, ColumnDef, ActionDef } from '../shared/admin-table.component';
import { AdminModalComponent } from '../shared/admin-modal.component';

@Component({
  selector: 'app-admin-coupons',
  standalone: true,
  imports: [CommonModule, FormsModule, AdminTableComponent, AdminModalComponent],
  template: `
    <div class="admin-coupons">
      <header class="section-header">
        <h2>Coupons</h2>
        <button class="btn btn-primary" (click)="openAddCoupon()">Add Coupon</button>
      </header>

      <app-admin-table
        [data]="coupons()"
        [columns]="couponColumns"
        [actions]="couponActions"
        [emptyMessage]="'No coupons found'"
      />

      @if (showCouponModal()) {
        <app-admin-modal
          [isOpen]="showCouponModal()"
          [title]="editingCoupon() ? 'Edit Coupon' : 'Add Coupon'"
          [loading]="saving()"
          (close)="closeCouponModal()"
          (confirm)="saveCoupon()">
          <form class="coupon-form">
            <div class="form-row">
              <div class="form-group">
                <label>Code *</label>
                <input type="text" [(ngModel)]="couponForm.code" name="code" required style="text-transform: uppercase;">
              </div>
              <div class="form-group">
                <label>Type *</label>
                <select [(ngModel)]="couponForm.type" name="type" required>
                  <option value="Percentage">Percentage</option>
                  <option value="Fixed">Fixed Amount</option>
                </select>
              </div>
            </div>
            <div class="form-group">
              <label>Description</label>
              <textarea [(ngModel)]="couponForm.description" name="description" rows="2"></textarea>
            </div>
            <div class="form-row">
              <div class="form-group">
                <label>Value *</label>
                <input type="number" [(ngModel)]="couponForm.value" name="value" step="0.01" min="0" required>
                <small class="hint">{{ couponForm.type === 'Percentage' ? '%' : '₹' }}</small>
              </div>
              <div class="form-group">
                <label>Min Cart Value</label>
                <input type="number" [(ngModel)]="couponForm.minCartValue" name="minCartValue" step="0.01" min="0">
              </div>
            </div>
            <div class="form-row">
              <div class="form-group">
                <label>Max Uses (0 = unlimited)</label>
                <input type="number" [(ngModel)]="couponForm.maxUses" name="maxUses" min="0">
              </div>
              <div class="form-group">
                <label>Expires At</label>
                <input type="datetime-local" [(ngModel)]="couponForm.expiresAt" name="expiresAt">
              </div>
            </div>
            <div class="form-group">
              <label>
                <input type="checkbox" [(ngModel)]="couponForm.isActive" name="isActive"> Active
              </label>
            </div>
          </form>
        </app-admin-modal>
      }
    </div>
  `,
  styles: [`
    .admin-coupons { padding: 1.5rem; }
    .section-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1.5rem; }
    .section-header h2 { margin: 0; font-size: 1.5rem; font-weight: 600; }
    .btn { padding: 0.625rem 1.25rem; border: none; border-radius: 6px; font-weight: 500; cursor: pointer; }
    .btn-primary { background: var(--primary, #2874F0); color: white; }
    .coupon-form { display: flex; flex-direction: column; gap: 1rem; }
    .form-row { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; }
    .form-group { display: flex; flex-direction: column; gap: 0.375rem; }
    .form-group label { font-weight: 500; font-size: 0.875rem; }
    .form-group input, .form-group select, .form-group textarea { padding: 0.5rem; border: 1px solid var(--border-color, #ddd); border-radius: 6px; font-size: 1rem; }
    .hint { color: var(--on-surface-variant, #666); font-size: 0.75rem; }
  `]
})
export class AdminCouponsComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly couponService = inject(CouponService);
  private readonly notificationService = inject(NotificationService);

  coupons = signal<Coupon[]>([]);

  showCouponModal = signal(false);
  editingCoupon = signal<Coupon | null>(null);
  couponForm: CreateCoupon = { code: '', description: '', type: 'Percentage', value: 10, minCartValue: 0, maxUses: 0, expiresAt: '', isActive: true };
  saving = signal(false);

  couponColumns: ColumnDef<Coupon>[] = [
    { key: 'id', header: 'ID', render: (c) => `#${c.id}` },
    { key: 'code', header: 'Code' },
    { key: 'type', header: 'Type' },
    { key: 'value', header: 'Value', render: (c) => c.type === 'Percentage' ? `${c.value}%` : `₹${c.value}` },
    { key: 'minCartValue', header: 'Min Cart', render: (c) => `₹${c.minCartValue}` },
    { key: 'maxUses', header: 'Max Uses', render: (c) => c.maxUses > 0 ? c.maxUses.toString() : 'Unlimited' },
    { key: 'currentUses', header: 'Used' },
    { key: 'isActive', header: 'Status', render: (c) => c.isActive ? 'Active' : 'Inactive' },
    { key: 'expiresAt', header: 'Expires', render: (c) => c.expiresAt ? new Date(c.expiresAt).toLocaleDateString() : 'Never' }
  ];

  couponActions: ActionDef<Coupon>[] = [
    { label: 'Edit', action: (c) => this.openEditCoupon(c), class: 'primary' },
    { label: 'Delete', action: (c) => this.deleteCoupon(c), class: 'danger' }
  ];

  ngOnInit(): void {
    this.loadCoupons();
  }

  loadCoupons(): void {
    this.couponService.getAll().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => this.coupons.set(data),
      error: () => this.notificationService.showError('Failed to load coupons')
    });
  }

  openAddCoupon(): void {
    this.editingCoupon.set(null);
    this.couponForm = { code: '', description: '', type: 'Percentage', value: 10, minCartValue: 0, maxUses: 0, expiresAt: '', isActive: true };
    this.showCouponModal.set(true);
  }

  openEditCoupon(coupon: Coupon): void {
    this.editingCoupon.set(coupon);
    this.couponForm = {
      code: coupon.code,
      description: coupon.description,
      type: coupon.type,
      value: coupon.value,
      minCartValue: coupon.minCartValue,
      maxUses: coupon.maxUses,
      expiresAt: coupon.expiresAt ? new Date(coupon.expiresAt).toISOString().slice(0, 16) : '',
      isActive: coupon.isActive
    };
    this.showCouponModal.set(true);
  }

  closeCouponModal(): void {
    this.showCouponModal.set(false);
    this.editingCoupon.set(null);
  }

  saveCoupon(): void {
    if (!this.couponForm.code || this.couponForm.value <= 0) {
      this.notificationService.showError('Code and valid value required');
      return;
    }
    this.saving.set(true);

    const payload: CreateCoupon = {
      code: this.couponForm.code.toUpperCase(),
      description: this.couponForm.description,
      type: this.couponForm.type,
      value: this.couponForm.value,
      minCartValue: this.couponForm.minCartValue,
      maxUses: this.couponForm.maxUses || undefined,
      expiresAt: this.couponForm.expiresAt ? new Date(this.couponForm.expiresAt).toISOString() : undefined,
      isActive: this.couponForm.isActive
    };

    if (this.editingCoupon()) {
      this.couponService.update(this.editingCoupon()!.id, payload).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: () => {
          this.notificationService.showSuccess('Coupon updated');
          this.closeCouponModal();
          this.loadCoupons();
          this.saving.set(false);
        },
        error: (err) => {
          this.notificationService.showError(err.error?.error || 'Failed to update coupon');
          this.saving.set(false);
        }
      });
    } else {
      this.couponService.create(payload).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: () => {
          this.notificationService.showSuccess('Coupon created');
          this.closeCouponModal();
          this.loadCoupons();
          this.saving.set(false);
        },
        error: (err) => {
          this.notificationService.showError(err.error?.error || 'Failed to create coupon');
          this.saving.set(false);
        }
      });
    }
  }

  deleteCoupon(coupon: Coupon): void {
    if (!confirm('Delete this coupon?')) return;
    this.couponService.delete(coupon.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.notificationService.showSuccess('Coupon deleted');
        this.coupons.update(c => c.filter(x => x.id !== coupon.id));
      },
      error: (err) => this.notificationService.showError(err.error?.error || 'Failed to delete coupon')
    });
  }
}