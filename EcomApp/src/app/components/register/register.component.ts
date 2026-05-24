import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { CartService } from '../../services/cart.service';
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
  private readonly cartService = inject(CartService);
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

    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(this.registerData.email)) {
      this.notificationService.showError('Please enter a valid email address');
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

    const payload = { ...this.registerData };
    if (!payload.phone) delete payload.phone;
    if (!payload.firstName) delete payload.firstName;
    if (!payload.lastName) delete payload.lastName;
    this.isLoading = true;
    this.authService.register(payload).subscribe({
      next: () => {
        this.notificationService.showSuccess('Registration successful!');
        this.cartService.mergeCart().subscribe({
          next: () => this.cartService.getCart().subscribe()
        });
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
