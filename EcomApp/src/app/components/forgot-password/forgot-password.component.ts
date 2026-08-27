import { Component, inject, signal, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { NotificationService } from '../../services/notification.service';

@Component({
  selector: 'app-forgot-password',
  imports: [FormsModule, RouterLink],
  templateUrl: './forgot-password.component.html',
  styleUrl: './forgot-password.component.scss'
})
export class ForgotPasswordComponent {
  private readonly authService = inject(AuthService);
  readonly notificationService = inject(NotificationService);
  private readonly destroyRef = inject(DestroyRef);

  email = '';
  isLoading = false;
  sent = signal(false);

  onSubmit(): void {
    if (!this.email?.trim()) {
      this.notificationService.showError('Please enter your email');
      return;
    }

    this.isLoading = true;
    this.authService.forgotPassword(this.email).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => this.handleSent(),
      error: () => this.handleSent()
    });
  }

  private handleSent(): void {
    this.isLoading = false;
    this.sent.set(true);
    this.notificationService.showSuccess('If the account exists, a reset link has been sent to your email.');
  }
}
