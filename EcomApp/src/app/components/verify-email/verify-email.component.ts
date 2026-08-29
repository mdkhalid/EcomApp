import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { NotificationService } from '../../services/notification.service';

@Component({
  selector: 'app-verify-email',
  imports: [RouterLink],
  templateUrl: './verify-email.component.html',
  styleUrl: './verify-email.component.scss'
})
export class VerifyEmailComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly authService = inject(AuthService);
  readonly notificationService = inject(NotificationService);

  status = signal<'verifying' | 'success' | 'error'>('verifying');

  constructor() {
    const token = this.route.snapshot.queryParamMap.get('token') ?? '';
    const email = this.route.snapshot.queryParamMap.get('email') ?? '';

    if (!token || !email) {
      this.status.set('error');
      return;
    }

    this.authService.verifyEmail({ email, token }).subscribe({
      next: () => {
        this.status.set('success');
        this.notificationService.showSuccess('Email verified! You can now place orders.');
      },
      error: () => this.status.set('error')
    });
  }
}
