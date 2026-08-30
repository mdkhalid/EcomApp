import { Component, inject, signal, OnInit, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BannerService } from '../../../services/banner.service';
import { NotificationService } from '../../../services/notification.service';
import { Banner, CreateBanner, UpdateBanner } from '../../../models/banner.model';
import { AdminTableComponent, ColumnDef, ActionDef } from '../shared/admin-table.component';
import { AdminModalComponent } from '../shared/admin-modal.component';

@Component({
  selector: 'app-admin-banners',
  standalone: true,
  imports: [CommonModule, FormsModule, AdminTableComponent, AdminModalComponent],
  template: `
    <div class="admin-banners">
      <header class="section-header">
        <h2>Banners</h2>
        <button class="btn btn-primary" (click)="openAddBanner()">Add Banner</button>
      </header>

      <app-admin-table
        [data]="banners()"
        [columns]="bannerColumns"
        [actions]="bannerActions"
        [emptyMessage]="'No banners found'"
      />

      @if (showBannerModal()) {
        <app-admin-modal
          [isOpen]="showBannerModal()"
          [title]="editingBanner() ? 'Edit Banner' : 'Add Banner'"
          [loading]="saving()"
          (close)="closeBannerModal()"
          (confirm)="saveBanner()">
          <form class="banner-form">
            <div class="form-group">
              <label>Title *</label>
              <input type="text" [(ngModel)]="bannerForm.title" name="title" required>
            </div>
            <div class="form-group">
              <label>Subtitle</label>
              <input type="text" [(ngModel)]="bannerForm.subtitle" name="subtitle">
            </div>
            <div class="form-row">
              <div class="form-group">
                <label>Background Gradient</label>
                <input type="text" [(ngModel)]="bannerForm.bgGradient" name="bgGradient" placeholder="linear-gradient(135deg, #2874F0, #1a5dc8)">
              </div>
              <div class="form-group">
                <label>Icon</label>
                <input type="text" [(ngModel)]="bannerForm.icon" name="icon" placeholder="e.g., devices, fashion">
              </div>
            </div>
            <div class="form-row">
              <div class="form-group">
                <label>Start Date *</label>
                <input type="date" [(ngModel)]="bannerForm.startDate" name="startDate" required>
              </div>
              <div class="form-group">
                <label>Duration (days) *</label>
                <input type="number" [(ngModel)]="bannerForm.durationDays" name="durationDays" min="1" required>
              </div>
            </div>
            <div class="form-row">
              <div class="form-group">
                <label>Sort Order</label>
                <input type="number" [(ngModel)]="bannerForm.sortOrder" name="sortOrder" min="0">
              </div>
              <div class="form-group">
                <label>Status</label>
                <select [(ngModel)]="bannerForm.isActive" name="isActive">
                  <option [value]="true">Active</option>
                  <option [value]="false">Inactive</option>
                </select>
              </div>
            </div>
            <div class="form-group">
              <label>Banner Image</label>
              <input type="file" #fileInput accept="image/*" (change)="onFileSelected($event)" style="display: none" [attr.id]="'banner-file-' + modalId()">
              <label [attr.for]="'banner-file-' + modalId()" class="btn btn-secondary">Select Image</label>
              <span class="file-name">{{ bannerSelectedFile?.name || 'No file selected' }}</span>
            </div>
            @if (bannerImagePreview) {
              <div class="image-preview">
                <img [src]="bannerImagePreview" alt="Preview">
              </div>
            }
          </form>
        </app-admin-modal>
      }
    </div>
  `,
  styles: [`
    .admin-banners { padding: 1.5rem; }
    .section-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1.5rem; }
    .section-header h2 { margin: 0; font-size: 1.5rem; font-weight: 600; }
    .btn { padding: 0.625rem 1.25rem; border: none; border-radius: 6px; font-weight: 500; cursor: pointer; }
    .btn-primary { background: var(--primary, #2874F0); color: white; }
    .btn-secondary { background: var(--secondary, #6c757d); color: white; }
    .banner-form { display: flex; flex-direction: column; gap: 1rem; }
    .form-row { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; }
    .form-group { display: flex; flex-direction: column; gap: 0.375rem; }
    .form-group label { font-weight: 500; font-size: 0.875rem; }
    .form-group input, .form-group select { padding: 0.5rem; border: 1px solid var(--border-color, #ddd); border-radius: 6px; font-size: 1rem; }
    .file-name { color: var(--on-surface-variant, #666); font-size: 0.875rem; }
    .image-preview { max-width: 200px; border-radius: 8px; overflow: hidden; }
    .image-preview img { width: 100%; height: auto; }
  `]
})
export class AdminBannersComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly bannerService = inject(BannerService);
  private readonly notificationService = inject(NotificationService);

  banners = signal<Banner[]>([]);

  showBannerModal = signal(false);
  editingBanner = signal<Banner | null>(null);
  bannerForm: CreateBanner = { title: '', subtitle: '', bgGradient: 'linear-gradient(135deg, #2874F0, #1a5dc8)', icon: 'devices', startDate: new Date().toISOString().split('T')[0], durationDays: 7, sortOrder: 1, isActive: true };
  bannerSelectedFile: File | null = null;
  bannerImagePreview: string | null = null;
  saving = signal(false);
  modalId = signal(Date.now());

  bannerColumns: ColumnDef<Banner>[] = [
    { key: 'id', header: 'ID', render: (b) => `#${b.id}` },
    { key: 'title', header: 'Title' },
    { key: 'subtitle', header: 'Subtitle' },
    { key: 'startDate', header: 'Start Date', render: (b) => new Date(b.startDate).toLocaleDateString() },
    { key: 'durationDays', header: 'Duration', render: (b) => `${b.durationDays} days` },
    { key: 'isActive', header: 'Status', render: (b) => b.isActive ? 'Active' : 'Inactive' }
  ];

  bannerActions: ActionDef<Banner>[] = [
    { label: 'Edit', action: (b) => this.openEditBanner(b), class: 'primary' },
    { label: 'Delete', action: (b) => this.deleteBanner(b), class: 'danger' },
    { label: 'Toggle', action: (b) => this.toggleBannerStatus(b), class: 'secondary' }
  ];

  ngOnInit(): void {
    this.loadBanners();
  }

  loadBanners(): void {
    this.bannerService.getAll().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => this.banners.set(data),
      error: () => this.notificationService.showError('Failed to load banners')
    });
  }

  openAddBanner(): void {
    this.editingBanner.set(null);
    this.bannerForm = { title: '', subtitle: '', bgGradient: 'linear-gradient(135deg, #2874F0, #1a5dc8)', icon: 'devices', startDate: new Date().toISOString().split('T')[0], durationDays: 7, sortOrder: 1, isActive: true };
    this.bannerSelectedFile = null;
    this.bannerImagePreview = null;
    this.modalId.set(Date.now());
    this.showBannerModal.set(true);
  }

  openEditBanner(banner: Banner): void {
    this.editingBanner.set(banner);
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
    this.modalId.set(Date.now());
    this.showBannerModal.set(true);
  }

  closeBannerModal(): void {
    this.showBannerModal.set(false);
    this.editingBanner.set(null);
  }

  onFileSelected(event: Event): void {
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
    this.saving.set(true);
    const payload: CreateBanner = { ...this.bannerForm, startDate: new Date(this.bannerForm.startDate).toISOString() };

    if (this.editingBanner()) {
      this.bannerService.update(this.editingBanner()!.id, payload as UpdateBanner).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: (updated) => {
          if (this.bannerSelectedFile) {
            this.uploadBannerImage(updated.id);
          } else {
            this.notificationService.showSuccess('Banner updated');
            this.closeBannerModal();
            this.loadBanners();
            this.saving.set(false);
          }
        },
        error: (err) => {
          this.notificationService.showError(err.error?.error || 'Failed to update banner');
          this.saving.set(false);
        }
      });
    } else {
      this.bannerService.create(payload).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: (created) => {
          if (this.bannerSelectedFile) {
            this.uploadBannerImage(created.id);
          } else {
            this.notificationService.showSuccess('Banner created');
            this.closeBannerModal();
            this.loadBanners();
            this.saving.set(false);
          }
        },
        error: (err) => {
          this.notificationService.showError(err.error?.error || 'Failed to create banner');
          this.saving.set(false);
        }
      });
    }
  }

  private uploadBannerImage(bannerId: number): void {
    const formData = new FormData();
    formData.append('file', this.bannerSelectedFile!);
    this.bannerService.uploadImage(bannerId, formData).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.notificationService.showSuccess('Banner saved with image');
        this.closeBannerModal();
        this.loadBanners();
        this.saving.set(false);
      },
      error: () => {
        this.notificationService.showSuccess('Banner saved, image upload failed');
        this.closeBannerModal();
        this.loadBanners();
        this.saving.set(false);
      }
    });
  }

  deleteBanner(banner: Banner): void {
    if (!confirm('Delete this banner?')) return;
    this.bannerService.delete(banner.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.notificationService.showSuccess('Banner deleted');
        this.banners.update(b => b.filter(x => x.id !== banner.id));
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
    this.bannerService.update(banner.id, updated).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.notificationService.showSuccess(`Banner ${updated.isActive ? 'activated' : 'deactivated'}`);
        this.loadBanners();
      },
      error: (err) => this.notificationService.showError(err.error?.error || 'Failed to toggle banner')
    });
  }

  getFullImageUrl(path: string): string {
    return `http://localhost:5068${path}`;
  }
}