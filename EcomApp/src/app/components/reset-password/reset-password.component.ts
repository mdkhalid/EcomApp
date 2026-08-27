import { Component, inject, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { NotificationService } from '../../services/notification.service';

@Component({
  selector: 'app-reset-password',
  imports: [FormsModule, RouterLink],
  templateUrl: './reset-password.component.html',
  styleUrl: './reset-password.component.scss'
})
export class ResetPasswordComponent {
  private readonly authService = inject(AuthService);
  readonly notificationService = inject(NotificationService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  email = '';
  token = '';
  newPassword = '';
  confirmPassword = '';
  isLoading = false;
  errors: Record<string, string> = {};

  constructor() {
    this.email = this.route.snapshot.queryParamMap.get('email') ?? '';
    this.token = this.route.snapshot.queryParamMap.get('token') ?? '';
  }

  onSubmit(): void {
    this.errors = {};
    if (!this.token) this.errors['token'] = 'Missing or invalid reset token.';
    if (!this.newPassword || this.newPassword.length < 8) this.errors['newPassword'] = 'Password must be at least 8 characters.';
    if (this.newPassword !== this.confirmPassword) this.errors['confirmPassword'] = 'Passwords do not match.';
    if (Object.keys(this.errors).length) return;

    this.isLoading = true;
    this.authService.resetPassword({ email: this.email, token: this.token, newPassword: this.newPassword })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.isLoading = false;
          this.notificationService.showSuccess('Password reset successful. Please log in.');
          this.router.navigate(['/login']);
        },
        error: (err) => {
          this.isLoading = false;
          this.errors['general'] = err.error?.error || 'Reset failed. The link may have expired.';
          this.notificationService.showError(this.errors['general'] ?? 'Reset failed.');
        }
      });
  }
}
