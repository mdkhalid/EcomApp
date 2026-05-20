import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { NotificationService } from '../../services/notification.service';
import { RegisterRequest } from '../../models/auth.model';

@Component({
  selector: 'app-register',
  imports: [FormsModule, RouterLink],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss'
})
export class RegisterComponent {
  private readonly authService = inject(AuthService);
  readonly notificationService = inject(NotificationService);
  private readonly router = inject(Router);

  registerData: RegisterRequest = {
    email: '',
    username: '',
    password: '',
    confirmPassword: '',
    firstName: '',
    lastName: '',
    phone: ''
  };
  isLoading = false;

  onSubmit(): void {
    if (!this.registerData.email || !this.registerData.username || !this.registerData.password) {
      this.notificationService.showError('Please fill in all required fields');
      return;
    }

    if (this.registerData.password !== this.registerData.confirmPassword) {
      this.notificationService.showError('Passwords do not match');
      return;
    }

    if (this.registerData.password.length < 8) {
      this.notificationService.showError('Password must be at least 8 characters');
      return;
    }

    this.isLoading = true;
    this.authService.register(this.registerData).subscribe({
      next: () => {
        this.notificationService.showSuccess('Registration successful!');
        this.router.navigate(['/products']);
      },
      error: (err) => {
        this.isLoading = false;
        const message = err.error?.error || 'Registration failed. Please try again.';
        this.notificationService.showError(message);
      }
    });
  }
}
