import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { NotificationService } from '../../services/notification.service';
import { User } from '../../models/auth.model';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.scss'
})
export class ProfileComponent implements OnInit {
  private readonly authService = inject(AuthService);
  readonly notification = inject(NotificationService);
  private readonly router = inject(Router);
  protected readonly notifications = this.notification.notifications;

  user = signal<User | null>(null);
  loading = signal(true);
  saving = signal(false);

  editForm = signal({
    firstName: '',
    lastName: '',
    phone: ''
  });

  ngOnInit(): void {
    if (!this.authService.isAuthenticated()) {
      this.router.navigate(['/login']);
      return;
    }
    this.loadProfile();
  }

  loadProfile(): void {
    this.loading.set(true);
    this.authService.getProfile().subscribe({
      next: (user) => {
        this.user.set(user);
        this.editForm.set({
          firstName: user.firstName || '',
          lastName: user.lastName || '',
          phone: user.phone || ''
        });
        this.loading.set(false);
      },
      error: () => {
        this.notification.showError('Failed to load profile');
        this.loading.set(false);
      }
    });
  }

  getInitials(): string {
    const u = this.user();
    if (!u) return '?';
    if (u.firstName && u.lastName) return (u.firstName[0] + u.lastName[0]).toUpperCase();
    return u.username[0].toUpperCase();
  }

  saveProfile(): void {
    this.saving.set(true);
    this.authService.updateProfile({
      firstName: this.editForm().firstName,
      lastName: this.editForm().lastName,
      phone: this.editForm().phone
    }).subscribe({
      next: (user) => {
        this.user.set(user);
        this.notification.showSuccess('Profile updated successfully!');
        // Refresh the auth service user data
        this.authService.getProfile().subscribe();
        this.saving.set(false);
      },
      error: (err) => {
        this.saving.set(false);
        this.notification.showError(err.error?.error || 'Failed to update profile');
      }
    });
  }

  getFullImageUrl(path: string): string {
    return `http://localhost:5068${path}`;
  }
}
