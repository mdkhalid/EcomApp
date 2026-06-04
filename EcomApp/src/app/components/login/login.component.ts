import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { CartService } from '../../services/cart.service';
import { NotificationService } from '../../services/notification.service';
import { LoginRequest } from '../../models/auth.model';

@Component({
  selector: 'app-login',
  imports: [FormsModule, RouterLink],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent {
  private readonly authService = inject(AuthService);
  private readonly cartService = inject(CartService);
  readonly notificationService = inject(NotificationService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  loginData: LoginRequest = { emailOrUsername: '', password: '' };
  isLoading = false;
  lockoutMessage = signal<string | null>(null);
  lockoutCountdown = signal<number>(0);
  private lockoutTimer: ReturnType<typeof setInterval> | null = null;

  onSubmit(): void {
    if (!this.loginData.emailOrUsername || !this.loginData.password) {
      this.notificationService.showError('Please fill in all fields');
      return;
    }

    this.clearLockout();
    this.isLoading = true;
    this.authService.login(this.loginData).subscribe({
      next: () => {
        this.isLoading = false;
        this.notificationService.showSuccess('Login successful!');
        if (!this.authService.isAdmin()) {
          this.cartService.mergeCart().subscribe({
            next: () => this.cartService.getCart().subscribe()
          });
        }
        const returnUrl = this.route.snapshot.queryParams['returnUrl'];
        if (returnUrl && returnUrl !== '/admin') {
          this.router.navigate([returnUrl]);
        } else if (this.authService.isAdmin()) {
          this.router.navigate(['/admin']);
        } else {
          this.router.navigate(['/products']);
        }
      },
      error: (err) => {
        this.isLoading = false;
        if (err.status === 423 && err.error?.lockoutEnd) {
          this.startLockoutCountdown(err.error.remainingSeconds ?? 0, err.error.error);
        } else {
          const message = err.error?.error || 'Login failed. Please try again.';
          this.notificationService.showError(message);
        }
      }
    });
  }

  private startLockoutCountdown(remainingSeconds: number, message: string): void {
    this.lockoutMessage.set(message);
    this.lockoutCountdown.set(remainingSeconds);
    this.loginData.password = '';

    if (this.lockoutTimer) clearInterval(this.lockoutTimer);
    this.lockoutTimer = setInterval(() => {
      const next = this.lockoutCountdown() - 1;
      if (next <= 0) {
        this.clearLockout();
      } else {
        this.lockoutCountdown.set(next);
      }
    }, 1000);
  }

  private clearLockout(): void {
    if (this.lockoutTimer) {
      clearInterval(this.lockoutTimer);
      this.lockoutTimer = null;
    }
    this.lockoutMessage.set(null);
    this.lockoutCountdown.set(0);
  }

  formatCountdown(seconds: number): string {
    const m = Math.floor(seconds / 60);
    const s = seconds % 60;
    return `${m}:${s.toString().padStart(2, '0')}`;
  }

  ngOnDestroy(): void {
    this.clearLockout();
  }
}
