import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';
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
  readonly notificationService = inject(NotificationService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  loginData: LoginRequest = { emailOrUsername: '', password: '' };
  isLoading = false;

  onSubmit(): void {
    if (!this.loginData.emailOrUsername || !this.loginData.password) {
      this.notificationService.showError('Please fill in all fields');
      return;
    }

    this.isLoading = true;
    this.authService.login(this.loginData).subscribe({
      next: () => {
        this.notificationService.showSuccess('Login successful!');
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
        console.error('Login error:', err);
        const message = err.error?.error || 'Login failed. Please try again.';
        this.notificationService.showError(message);
      }
    });
  }
}
