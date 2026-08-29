import { Component, inject, signal, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
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
  private readonly destroyRef = inject(DestroyRef);
  private readonly authService = inject(AuthService);
  private readonly cartService = inject(CartService);
  readonly notificationService = inject(NotificationService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  loginData: LoginRequest = { emailOrUsername: '', password: '' };
  isLoading = false;
  awaitingTwoFactor = signal(false);
  pendingTwoFactorToken = '';
  twoFactorCode = '';
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
    this.authService.login(this.loginData).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (response: any) => {
        this.isLoading = false;
        if (response?.requiresTwoFactor) {
          this.pendingTwoFactorToken = response.twoFactorToken;
          this.awaitingTwoFactor.set(true);
          return;
        }
        this.onLoginSuccess();
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

  submitTwoFactor(): void {
    if (!this.twoFactorCode?.trim()) {
      this.notificationService.showError('Please enter the 6-digit code');
      return;
    }
    this.isLoading = true;
    this.authService.validateTwoFactor({ twoFactorToken: this.pendingTwoFactorToken, code: this.twoFactorCode })
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: () => {
          this.isLoading = false;
          this.onLoginSuccess();
        },
        error: (err) => {
          this.isLoading = false;
          this.twoFactorCode = '';
          const message = err.error?.error || 'Invalid verification code.';
          this.notificationService.showError(message);
        }
      });
  }

  private onLoginSuccess(): void {
    this.notificationService.showSuccess('Login successful!');
    if (!this.authService.isAdmin()) {
      this.cartService.mergeCart().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: () => this.cartService.getCart().pipe(takeUntilDestroyed(this.destroyRef)).subscribe()
      });
    }
    const returnUrl = this.route.snapshot.queryParams['returnUrl'];
    if (returnUrl && this.isSafeRedirect(returnUrl)) {
      this.router.navigate([returnUrl]);
    } else if (this.authService.isAdmin()) {
      this.router.navigate(['/admin']);
    } else {
      this.router.navigate(['/products']);
    }
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

  private isSafeRedirect(url: string): boolean {
    return url.startsWith('/') && !url.startsWith('//') && !url.includes('://');
  }
}
