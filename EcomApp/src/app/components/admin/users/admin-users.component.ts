import { Component, inject, signal, OnInit, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { API_URL } from '../../../utils/api-config';
import { NotificationService } from '../../../services/notification.service';
import { User, CreateUserRequest, AdminChangePasswordRequest } from '../../../models/auth.model';
import { AdminTableComponent, ColumnDef, ActionDef } from '../shared/admin-table.component';
import { AdminModalComponent } from '../shared/admin-modal.component';

interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [CommonModule, FormsModule, AdminTableComponent, AdminModalComponent],
  template: `
    <div class="admin-users">
      <header class="section-header">
        <h2>Users</h2>
        <button class="btn btn-primary" (click)="openCreateUser()">Add User</button>
      </header>

      <div class="filters">
        <input type="text" [(ngModel)]="search" (ngModelChange)="loadUsers()" placeholder="Search users..." class="search-input">
        <select [(ngModel)]="roleFilter" (ngModelChange)="loadUsers()" class="filter-select">
          <option value="">All Roles</option>
          @for (role of roles(); track role) {
            <option [value]="role">{{ role }}</option>
          }
        </select>
        <div class="pagination-controls" *ngIf="pagination">
          <button class="btn btn-sm" (click)="prevPage()" [disabled]="currentPage <= 1">Previous</button>
          <span>Page {{ currentPage }} of {{ totalPages }} ({{ totalItems }} total)</span>
          <button class="btn btn-sm" (click)="nextPage()" [disabled]="currentPage >= totalPages">Next</button>
        </div>
      </div>

      <app-admin-table
        [data]="users()"
        [columns]="userColumns"
        [actions]="userActions"
        [emptyMessage]="'No users found'"
      />

      @if (showCreateUserModal()) {
        <app-admin-modal
          [isOpen]="showCreateUserModal()"
          [title]="'Create User'"
          [loading]="creating()"
          (close)="closeCreateUser()"
          (confirm)="saveCreateUser()">
          <form class="user-form">
            <div class="form-row">
              <div class="form-group">
                <label>Email *</label>
                <input type="email" [(ngModel)]="createUserForm.email" name="email" required>
                @if (createUserErrors['email']) {
                  <span class="error">{{ createUserErrors['email'] }}</span>
                }
              </div>
              <div class="form-group">
                <label>Username *</label>
                <input type="text" [(ngModel)]="createUserForm.username" name="username" required>
                @if (createUserErrors['username']) {
                  <span class="error">{{ createUserErrors['username'] }}</span>
                }
              </div>
            </div>
            <div class="form-row">
              <div class="form-group">
                <label>Password *</label>
                <input type="password" [(ngModel)]="createUserForm.password" name="password" required>
                @if (createUserErrors['password']) {
                  <span class="error">{{ createUserErrors['password'] }}</span>
                }
              </div>
              <div class="form-group">
                <label>Confirm Password *</label>
                <input type="password" [(ngModel)]="createUserForm.confirmPassword" name="confirmPassword" required>
                @if (createUserErrors['confirmPassword']) {
                  <span class="error">{{ createUserErrors['confirmPassword'] }}</span>
                }
              </div>
            </div>
            <div class="form-row">
              <div class="form-group">
                <label>First Name</label>
                <input type="text" [(ngModel)]="createUserForm.firstName" name="firstName">
              </div>
              <div class="form-group">
                <label>Last Name</label>
                <input type="text" [(ngModel)]="createUserForm.lastName" name="lastName">
              </div>
            </div>
            <div class="form-row">
              <div class="form-group">
                <label>Phone</label>
                <input type="tel" [(ngModel)]="createUserForm.phone" name="phone">
              </div>
              <div class="form-group">
                <label>Role *</label>
                <select [(ngModel)]="createUserForm.role" name="role" required>
                  @for (r of roles(); track r) {
                    <option [value]="r">{{ r }}</option>
                  }
                </select>
                @if (createUserErrors['role']) {
                  <span class="error">{{ createUserErrors['role'] }}</span>
                }
              </div>
            </div>
          </form>
        </app-admin-modal>
      }

      @if (showChangePasswordModal()) {
        <app-admin-modal
          [isOpen]="showChangePasswordModal()"
          [title]="'Change Password for ' + (changePasswordUser()?.username || '')"
          [loading]="changingPassword()"
          (close)="closeChangePassword()"
          (confirm)="saveChangePassword()">
          <div class="password-form">
            <div class="form-group">
              <label>New Password *</label>
              <input type="password" [(ngModel)]="changePasswordForm.newPassword" name="newPassword" required minlength="8">
            </div>
            <div class="form-group">
              <label>Confirm New Password *</label>
              <input type="password" [(ngModel)]="changePasswordForm.confirmPassword" name="confirmPassword" required>
            </div>
          </div>
        </app-admin-modal>
      }
    </div>
  `,
  styles: [`
    .admin-users { padding: 1.5rem; }
    .section-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1.5rem; }
    .section-header h2 { margin: 0; font-size: 1.5rem; font-weight: 600; }
    .filters { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1.5rem; flex-wrap: wrap; gap: 1rem; }
    .search-input { flex: 1; min-width: 200px; padding: 0.5rem; border: 1px solid var(--border-color, #ddd); border-radius: 6px; }
    .filter-select { padding: 0.5rem; border: 1px solid var(--border-color, #ddd); border-radius: 6px; background: white; }
    .pagination-controls { display: flex; align-items: center; gap: 0.75rem; }
    .btn { padding: 0.625rem 1.25rem; border: none; border-radius: 6px; font-weight: 500; cursor: pointer; transition: all 0.2s; }
    .btn:disabled { opacity: 0.6; cursor: not-allowed; }
    .btn-primary { background: var(--primary, #2874F0); color: white; }
    .btn-secondary { background: var(--secondary, #6c757d); color: white; }
    .btn-danger { background: var(--danger, #dc3545); color: white; }
    .btn-sm { padding: 0.375rem 0.75rem; font-size: 0.875rem; }
    .user-form { display: flex; flex-direction: column; gap: 1rem; }
    .form-row { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; }
    .form-group { display: flex; flex-direction: column; gap: 0.375rem; }
    .form-group label { font-weight: 500; font-size: 0.875rem; }
    .form-group input, .form-group select { padding: 0.5rem; border: 1px solid var(--border-color, #ddd); border-radius: 6px; font-size: 1rem; }
    .error { color: var(--danger, #dc3545); font-size: 0.75rem; }
    .password-form { display: flex; flex-direction: column; gap: 1rem; }
  `]
})
export class AdminUsersComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly http = inject(HttpClient);
  private readonly apiUrl = API_URL;
  private readonly notificationService = inject(NotificationService);

  users = signal<User[]>([]);
  roles = signal<string[]>(['Customer', 'SubAdmin', 'Admin']);
  search = '';
  roleFilter = '';
  currentPage = 1;
  pageSize = 20;
  totalItems = 0;

  get totalPages(): number {
    return Math.ceil(this.totalItems / this.pageSize) || 1;
  }

  get pagination() {
    return { current: this.currentPage, total: this.totalPages };
  }

  showCreateUserModal = signal(false);
  createUserForm: CreateUserRequest = { email: '', username: '', password: '', confirmPassword: '', role: 'Customer', firstName: '', lastName: '', phone: '' };
  createUserErrors: { [key: string]: string } = {};
  creating = signal(false);

  showChangePasswordModal = signal(false);
  changePasswordUser = signal<User | null>(null);
  changePasswordForm: AdminChangePasswordRequest = { newPassword: '', confirmPassword: '' };
  changingPassword = signal(false);

  userColumns: ColumnDef<User>[] = [
    { key: 'id', header: 'ID', render: (u) => `#${u.id}` },
    { key: 'email', header: 'Email' },
    { key: 'username', header: 'Username' },
    { key: 'role', header: 'Role' },
    { key: 'isActive', header: 'Status', render: (u) => u.isActive ? 'Active' : 'Inactive' },
    { key: 'createdAt', header: 'Joined', render: (u) => new Date(u.createdAt).toLocaleDateString() }
  ];

  userActions: ActionDef<User>[] = [
    { label: 'Change Password', action: (u) => this.openChangePassword(u), class: 'secondary' },
    { label: 'Toggle Status', action: (u) => this.toggleStatus(u), class: 'secondary' }
  ];

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    const search = this.search;
    const role = this.roleFilter;
    let url = `${this.apiUrl}/auth/users?pageNumber=${this.currentPage}&pageSize=${this.pageSize}`;
    if (search) url += `&search=${encodeURIComponent(search)}`;
    if (role) url += `&role=${encodeURIComponent(role)}`;

    this.http.get<PaginatedResponse<User>>(url).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (res) => {
        this.users.set(res.items);
        this.totalItems = res.totalCount;
      },
      error: () => this.notificationService.showError('Failed to load users')
    });
  }

  prevPage(): void {
    if (this.currentPage > 1) {
      this.currentPage--;
      this.loadUsers();
    }
  }

  nextPage(): void {
    if (this.currentPage < this.totalPages) {
      this.currentPage++;
      this.loadUsers();
    }
  }

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

    if (!f.email?.trim()) errors['email'] = 'Email is required';
    else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(f.email)) errors['email'] = 'Please enter a valid email address';

    if (!f.username?.trim()) errors['username'] = 'Username is required';
    else if (f.username.length < 3) errors['username'] = 'Username must be at least 3 characters';

    if (!f.password) errors['password'] = 'Password is required';
    else if (f.password.length < 8) errors['password'] = 'Password must be at least 8 characters';

    if (!f.confirmPassword) errors['confirmPassword'] = 'Please confirm the password';
    else if (f.password !== f.confirmPassword) errors['confirmPassword'] = 'Passwords do not match';

    if (!f.role) errors['role'] = 'Role is required';

    this.createUserErrors = errors;
    if (Object.keys(errors).length > 0) {
      this.notificationService.showError('Please fix the highlighted fields');
      return;
    }

    this.creating.set(true);
    const payload = { ...f };
    if (!payload.phone) delete payload.phone;
    if (!payload.firstName) delete payload.firstName;
    if (!payload.lastName) delete payload.lastName;

    this.http.post<User>(`${this.apiUrl}/auth/users`, payload).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.notificationService.showSuccess('User created successfully');
        this.closeCreateUser();
        this.loadUsers();
        this.creating.set(false);
      },
      error: (err) => {
        this.notificationService.showError(err.error?.error || 'Failed to create user');
        this.creating.set(false);
      }
    });
  }

  toggleStatus(user: User): void {
    const action = user.isActive ? 'deactivate' : 'activate';
    this.http.put(`${this.apiUrl}/auth/users/${user.id}/${action}`, {}).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.notificationService.showSuccess(`User ${action}d`);
        this.loadUsers();
      },
      error: (err) => this.notificationService.showError(err.error?.error || `Failed to ${action} user`)
    });
  }

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

    this.changingPassword.set(true);
    this.http.post(`${this.apiUrl}/auth/users/${userId}/change-password`, f).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.notificationService.showSuccess('Password changed successfully');
        this.closeChangePassword();
        this.changingPassword.set(false);
      },
      error: (err) => {
        this.notificationService.showError(err.error?.error || 'Failed to change password');
        this.changingPassword.set(false);
      }
    });
  }
}