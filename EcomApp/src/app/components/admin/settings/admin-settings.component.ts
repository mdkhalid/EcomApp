import { Component, inject, signal, OnInit, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminSettingsService } from '../../../services/admin-settings.service';
import { NotificationService } from '../../../services/notification.service';
import { AdminSetting, SettingUpdate } from '../../../models/admin-settings.model';
import { AdminTableComponent, ColumnDef, ActionDef } from '../shared/admin-table.component';
import { AdminModalComponent } from '../shared/admin-modal.component';

@Component({
  selector: 'app-admin-settings',
  standalone: true,
  imports: [CommonModule, FormsModule, AdminTableComponent, AdminModalComponent],
  template: `
    <div class="admin-settings">
      <header class="section-header">
        <h2>Settings</h2>
        <button class="btn btn-secondary" (click)="loadSettings()">Refresh</button>
      </header>

      <div class="settings-description">
        <p>Manage application settings. Changes take effect immediately.</p>
        <p class="warning">⚠️ Be careful when modifying settings as they affect the entire application.</p>
      </div>

      <app-admin-table
        [data]="settingsList()"
        [columns]="settingsColumns"
        [actions]="settingsActions"
        [emptyMessage]="'No settings found'"
      />

      @if (showEditModal()) {
        <app-admin-modal
          [isOpen]="showEditModal()"
          [title]="'Edit Setting: ' + (editingSetting()?.key || '')"
          [loading]="saving()"
          (close)="closeEditModal()"
          (confirm)="saveSetting()">
          <div class="setting-edit-form">
            <div class="form-group">
              <label>Key</label>
              <input type="text" [value]="editingSetting()?.key" readonly class="readonly">
            </div>
            <div class="form-group">
              <label>Description</label>
              <input type="text" [value]="editingSetting()?.description" readonly class="readonly">
            </div>
            <div class="form-group">
              <label>Type</label>
              <input type="text" [value]="editingSetting()?.type" readonly class="readonly">
            </div>
            <div class="form-group">
              <label>Current Value *</label>
              <input type="text" [(ngModel)]="editValue" name="value" [placeholder]="editingSetting()?.value" required>
              <small class="hint">Leave empty to keep current value</small>
            </div>
            @if (editingSetting()?.isPublic === false) {
              <div class="warning-box">
                <strong>⚠️ Secret Setting:</strong> The actual value is not displayed for security. Enter the new value to update.
              </div>
            }
          </div>
        </app-admin-modal>
      }
    </div>
  `,
  styles: [`
    .admin-settings { padding: 1.5rem; }
    .section-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem; }
    .section-header h2 { margin: 0; font-size: 1.5rem; font-weight: 600; }
    .btn { padding: 0.625rem 1.25rem; border: none; border-radius: 6px; font-weight: 500; cursor: pointer; }
    .btn-secondary { background: var(--secondary, #6c757d); color: white; }
    .settings-description { margin-bottom: 1.5rem; padding: 1rem; background: var(--surface-variant, #fafafa); border-radius: 8px; }
    .settings-description p { margin: 0.5rem 0; }
    .settings-description .warning { color: var(--warning, #e65100); font-weight: 500; }
    .setting-edit-form { display: flex; flex-direction: column; gap: 1rem; }
    .form-group { display: flex; flex-direction: column; gap: 0.375rem; }
    .form-group label { font-weight: 500; font-size: 0.875rem; }
    .form-group input { padding: 0.5rem; border: 1px solid var(--border-color, #ddd); border-radius: 6px; font-size: 1rem; }
    .form-group input.readonly { background: var(--surface-variant, #f5f5f5); color: var(--on-surface-variant, #666); }
    .hint { color: var(--on-surface-variant, #666); font-size: 0.75rem; }
    .warning-box { padding: 0.75rem; background: #fff3e0; border: 1px solid #ffe0b2; border-radius: 6px; color: #e65100; font-size: 0.875rem; }
  `]
})
export class AdminSettingsComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly adminSettingsService = inject(AdminSettingsService);
  private readonly notificationService = inject(NotificationService);

  settingsList = signal<AdminSetting[]>([]);
  settingsLoading = signal(false);
  settingsSaving = signal(false);
  settingsDraft = signal<Record<string, string>>({});

  showEditModal = signal(false);
  editingSetting = signal<AdminSetting | null>(null);
  editValue = '';
  saving = signal(false);

  settingsColumns: ColumnDef<AdminSetting>[] = [
    { key: 'key', header: 'Key' },
    { key: 'description', header: 'Description' },
    { key: 'type', header: 'Type' },
    { key: 'isPublic', header: 'Visibility', render: (s) => s.isPublic === true ? 'Public' : 'Secret (Hidden)' },
    { key: 'value', header: 'Value', render: (s) => s.isPublic === true ? (s.value ?? '') : '********' }
  ];

  settingsActions: ActionDef<AdminSetting>[] = [
    { label: 'Edit', action: (s) => this.openEditSetting(s), class: 'primary' }
  ];

  ngOnInit(): void {
    this.loadSettings();
  }

  loadSettings(): void {
    this.settingsLoading.set(true);
    this.adminSettingsService.getAll().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        this.settingsList.set(data);
        this.settingsLoading.set(false);
      },
      error: () => {
        this.notificationService.showError('Failed to load settings');
        this.settingsLoading.set(false);
      }
    });
  }

  openEditSetting(setting: AdminSetting): void {
    this.editingSetting.set(setting);
    this.editValue = setting.isPublic === true ? (setting.value ?? '') : '';
    this.showEditModal.set(true);
  }

  closeEditModal(): void {
    this.showEditModal.set(false);
    this.editingSetting.set(null);
    this.editValue = '';
  }

  saveSetting(): void {
    const setting = this.editingSetting();
    if (!setting || !this.editValue.trim()) {
      this.notificationService.showError('Value is required');
      return;
    }

    this.saving.set(true);
    const updates: SettingUpdate[] = [{ key: setting!.key, value: this.editValue }];

    this.adminSettingsService.update(updates).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.notificationService.showSuccess('Setting updated successfully');
        this.closeEditModal();
        this.loadSettings();
        this.saving.set(false);
      },
      error: (err) => {
        this.notificationService.showError(err.error?.error || 'Failed to update setting');
        this.saving.set(false);
      }
    });
  }
}