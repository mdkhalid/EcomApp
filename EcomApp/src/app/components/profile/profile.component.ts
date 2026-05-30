import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { NotificationService } from '../../services/notification.service';
import { User, Address, CreateAddressRequest } from '../../models/auth.model';

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
    phone: '',
    gender: '',
    dateOfBirth: ''
  });

  passwordForm = signal({
    currentPassword: '',
    newPassword: '',
    confirmNewPassword: ''
  });

  changingPassword = signal(false);
  profilePictureUploading = signal(false);

  // Address book
  addresses = signal<Address[]>([]);
  showAddressForm = signal(false);
  editingAddress = signal<Address | null>(null);
  addressForm = signal({
    label: 'Home',
    street: '',
    city: '',
    state: '',
    zipCode: '',
    country: '',
    isDefault: false
  });
  addressSaving = signal(false);
  addressDeleting = signal<number | null>(null);

  // Profile completion
  profileCompletion = signal(0);

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
          phone: user.phone || '',
          gender: user.gender || '',
          dateOfBirth: user.dateOfBirth ? user.dateOfBirth.split('T')[0] : ''
        });
        this.loading.set(false);
        this.loadAddresses();
        this.calculateCompletion();
      },
      error: () => {
        this.notification.showError('Failed to load profile');
        this.loading.set(false);
      }
    });
  }

  loadAddresses(): void {
    this.authService.getAddresses().subscribe({
      next: (addresses) => {
        this.addresses.set(addresses);
        this.calculateCompletion();
      },
      error: () => {
        this.notification.showError('Failed to load addresses');
      }
    });
  }

  getInitials(): string {
    const user = this.user();
    if (!user) return '?';
    const first = user.firstName?.charAt(0) || '';
    const last = user.lastName?.charAt(0) || '';
    if (first || last) return (first + last).toUpperCase();
    return user.username.charAt(0).toUpperCase();
  }

  getFullImageUrl(path: string): string {
    if (!path) return '';
    if (path.startsWith('http')) return path;
    return `http://localhost:5068${path}`;
  }

  // Profile Picture
  onProfilePictureSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;

    const file = input.files[0];
    const allowedTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/webp'];
    if (!allowedTypes.includes(file.type)) {
      this.notification.showError('Invalid file type. Allowed: JPG, PNG, WebP');
      input.value = '';
      return;
    }

    if (file.size > 5 * 1024 * 1024) {
      this.notification.showError('File size exceeds 5MB limit');
      input.value = '';
      return;
    }

    this.profilePictureUploading.set(true);
    this.authService.uploadProfilePicture(file).subscribe({
      next: (user) => {
        this.user.set(user);
        this.profilePictureUploading.set(false);
        this.calculateCompletion();
        this.notification.showSuccess('Profile picture updated');
      },
      error: () => {
        this.profilePictureUploading.set(false);
        this.notification.showError('Failed to upload profile picture');
      }
    });
    input.value = '';
  }

  removeProfilePicture(): void {
    this.authService.removeProfilePicture().subscribe({
      next: (user) => {
        this.user.set(user);
        this.calculateCompletion();
        this.notification.showSuccess('Profile picture removed');
      },
      error: () => {
        this.notification.showError('Failed to remove profile picture');
      }
    });
  }

  // Profile Save
  saveProfile(): void {
    this.saving.set(true);
    const form = this.editForm();
    this.authService.updateProfile({
      firstName: form.firstName,
      lastName: form.lastName,
      phone: form.phone,
      gender: form.gender,
      dateOfBirth: form.dateOfBirth || undefined
    }).subscribe({
      next: (user) => {
        this.user.set(user);
        this.saving.set(false);
        this.calculateCompletion();
        this.notification.showSuccess('Profile updated successfully');
      },
      error: () => {
        this.saving.set(false);
        this.notification.showError('Failed to update profile');
      }
    });
  }

  // Password Change
  changePassword(): void {
    const form = this.passwordForm();
    if (form.newPassword !== form.confirmNewPassword) {
      this.notification.showError('New passwords do not match');
      return;
    }

    this.changingPassword.set(true);
    this.authService.changePassword({
      currentPassword: form.currentPassword,
      newPassword: form.newPassword,
      confirmNewPassword: form.confirmNewPassword
    }).subscribe({
      next: () => {
        this.changingPassword.set(false);
        this.passwordForm.set({ currentPassword: '', newPassword: '', confirmNewPassword: '' });
        this.notification.showSuccess('Password changed successfully. Please log in again.');
        setTimeout(() => {
          this.authService.logout();
          this.router.navigate(['/login']);
        }, 1500);
      },
      error: () => {
        this.changingPassword.set(false);
        this.notification.showError('Failed to change password. Check your current password.');
      }
    });
  }

  // Address Book
  openAddressForm(address?: Address): void {
    if (address) {
      this.editingAddress.set(address);
      this.addressForm.set({
        label: address.label,
        street: address.street,
        city: address.city,
        state: address.state,
        zipCode: address.zipCode,
        country: address.country,
        isDefault: address.isDefault
      });
    } else {
      this.editingAddress.set(null);
      this.addressForm.set({
        label: 'Home',
        street: '',
        city: '',
        state: '',
        zipCode: '',
        country: '',
        isDefault: false
      });
    }
    this.showAddressForm.set(true);
  }

  closeAddressForm(): void {
    this.showAddressForm.set(false);
    this.editingAddress.set(null);
  }

  saveAddress(): void {
    const form = this.addressForm();
    if (!form.street || !form.city || !form.state || !form.zipCode || !form.country) {
      this.notification.showError('Please fill in all address fields');
      return;
    }

    this.addressSaving.set(true);
    const editing = this.editingAddress();

    if (editing) {
      this.authService.updateAddress(editing.id, form).subscribe({
        next: () => {
          this.addressSaving.set(false);
          this.closeAddressForm();
          this.loadAddresses();
          this.notification.showSuccess('Address updated');
        },
        error: () => {
          this.addressSaving.set(false);
          this.notification.showError('Failed to update address');
        }
      });
    } else {
      this.authService.addAddress(form as CreateAddressRequest).subscribe({
        next: () => {
          this.addressSaving.set(false);
          this.closeAddressForm();
          this.loadAddresses();
          this.notification.showSuccess('Address added');
        },
        error: () => {
          this.addressSaving.set(false);
          this.notification.showError('Failed to add address');
        }
      });
    }
  }

  deleteAddress(id: number): void {
    this.addressDeleting.set(id);
    this.authService.deleteAddress(id).subscribe({
      next: () => {
        this.addressDeleting.set(null);
        this.loadAddresses();
        this.notification.showSuccess('Address deleted');
      },
      error: () => {
        this.addressDeleting.set(null);
        this.notification.showError('Failed to delete address');
      }
    });
  }

  setDefaultAddress(id: number): void {
    this.authService.updateAddress(id, { isDefault: true }).subscribe({
      next: () => {
        this.loadAddresses();
        this.notification.showSuccess('Default address updated');
      },
      error: () => {
        this.notification.showError('Failed to update default address');
      }
    });
  }

  // Profile Completion
  calculateCompletion(): void {
    const user = this.user();
    if (!user) {
      this.profileCompletion.set(0);
      return;
    }

    let filled = 0;
    const total = 7;

    if (user.firstName) filled++;
    if (user.lastName) filled++;
    if (user.phone) filled++;
    if (user.gender) filled++;
    if (user.dateOfBirth) filled++;
    if (user.profilePictureUrl) filled++;
    if (this.addresses().length > 0) filled++;

    this.profileCompletion.set(Math.round((filled / total) * 100));
  }

  getCompletionItems(): { label: string; filled: boolean }[] {
    const user = this.user();
    if (!user) return [];

    return [
      { label: 'First Name', filled: !!user.firstName },
      { label: 'Last Name', filled: !!user.lastName },
      { label: 'Phone', filled: !!user.phone },
      { label: 'Gender', filled: !!user.gender },
      { label: 'Date of Birth', filled: !!user.dateOfBirth },
      { label: 'Photo', filled: !!user.profilePictureUrl },
      { label: 'Address', filled: this.addresses().length > 0 }
    ];
  }
}
