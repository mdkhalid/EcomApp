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
  registerErrors: { [key: string]: string } = {};
  isLoading = false;

  onSubmit(): void {
    const d = this.registerData;
    const errors: { [key: string]: string } = {};

    if (!d.email?.trim()) {
      errors['email'] = 'Email is required';
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(d.email)) {
      errors['email'] = 'Please enter a valid email address';
    }

    if (!d.username?.trim()) {
      errors['username'] = 'Username is required';
    } else if (d.username.length < 3) {
      errors['username'] = 'Username must be at least 3 characters';
    }

    if (!d.password) {
      errors['password'] = 'Password is required';
    } else if (d.password.length < 8) {
      errors['password'] = 'Password must be at least 8 characters';
    }

    if (!d.confirmPassword) {
      errors['confirmPassword'] = 'Please confirm the password';
    } else if (d.password !== d.confirmPassword) {
      errors['confirmPassword'] = 'Passwords do not match';
    }

    this.registerErrors = errors;
    if (Object.keys(errors).length > 0) {
      this.notificationService.showError('Please fix the highlighted fields');
      return;
    }

    const payload = { ...d };
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

  clearRegisterError(field: string): void {
    if (this.registerErrors[field]) {
      delete this.registerErrors[field];
      this.registerErrors = { ...this.registerErrors };
    }
  }
}
