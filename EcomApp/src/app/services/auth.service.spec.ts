import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { AuthService } from './auth.service';
import { User, LoginRequest, RegisterRequest, TokenResponse, TwoFactorChallenge } from '../models/auth.model';
import { API_URL } from '../utils/api-config';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;
  const mockApiUrl = 'http://test-api.com';

  const mockUser: User = {
    id: 1,
    email: 'test@example.com',
    username: 'testuser',
    role: 'Customer',
    firstName: 'Test',
    lastName: 'User',
    emailVerified: true,
    createdAt: '2024-01-01T00:00:00Z'
  };

  const mockTokenResponse: TokenResponse = {
    accessToken: 'mock-access-token',
    refreshToken: 'mock-refresh-token',
    emailVerified: true
  };

  const mockTwoFactorChallenge: TwoFactorChallenge = {
    requiresTwoFactor: true,
    twoFactorToken: '2fa-token'
  };

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        AuthService,
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_URL, useValue: mockApiUrl }
      ]
    });

    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should initialize as not authenticated', () => {
    expect(service.isAuthenticated()).toBe(false);
    expect(service.currentUser()).toBeNull();
  });

  describe('register', () => {
    it('should register and set user data on success', () => {
      const registerData: RegisterRequest = {
        email: 'new@example.com',
        password: 'Password123!',
        confirmPassword: 'Password123!',
        firstName: 'New',
        lastName: 'User'
      };

      service.register(registerData).subscribe(response => {
        expect(response).toEqual(mockTokenResponse);
        expect(service.isAuthenticated()).toBe(true);
        expect(service.currentUser()).toBeTruthy();
      });

      const req = httpMock.expectOne(`${mockApiUrl}/auth/register`);
      expect(req.request.method).toBe('POST');
      req.flush(mockTokenResponse);
    });

    it('should handle registration error', () => {
      const registerData: RegisterRequest = {
        email: 'existing@example.com',
        password: 'Password123!',
        confirmPassword: 'Password123!',
        firstName: 'Test',
        lastName: 'User'
      };

      service.register(registerData).subscribe({
        next: () => fail('should have failed'),
        error: (error) => expect(error).toBeTruthy()
      });

      const req = httpMock.expectOne(`${mockApiUrl}/auth/register`);
      req.flush({ error: 'Email already exists' }, { status: 400, statusText: 'Bad Request' });
    });
  });

  describe('login', () => {
    it('should login and set user data on success', () => {
      const loginData: LoginRequest = {
        email: 'test@example.com',
        password: 'Password123!'
      };

      service.login(loginData).subscribe(response => {
        expect(response).toEqual(mockTokenResponse);
        expect(service.isAuthenticated()).toBe(true);
      });

      const req = httpMock.expectOne(`${mockApiUrl}/auth/login`);
      expect(req.request.method).toBe('POST');
      req.flush(mockTokenResponse);
    });

    it('should return 2FA challenge when required', () => {
      const loginData: LoginRequest = {
        email: 'admin@example.com',
        password: 'Password123!'
      };

      service.login(loginData).subscribe(response => {
        expect(response).toEqual(mockTwoFactorChallenge);
        expect(service.isAuthenticated()).toBe(false);
      });

      const req = httpMock.expectOne(`${mockApiUrl}/auth/login`);
      req.flush(mockTwoFactorChallenge);
    });
  });

  describe('logout', () => {
    it('should clear all stored data and signals', () => {
      localStorage.setItem('accessToken', 'token');
      localStorage.setItem('refreshToken', 'refresh');
      localStorage.setItem('user', JSON.stringify(mockUser));

      service.logout();

      expect(localStorage.getItem('accessToken')).toBeNull();
      expect(localStorage.getItem('refreshToken')).toBeNull();
      expect(localStorage.getItem('user')).toBeNull();
      expect(service.isAuthenticated()).toBe(false);
      expect(service.currentUser()).toBeNull();
    });
  });

  describe('changePassword', () => {
    it('should change password successfully', () => {
      service.changePassword({ currentPassword: 'old', newPassword: 'new' }).subscribe(response => {
        expect(response.message).toBe('Password changed successfully');
      });

      const req = httpMock.expectOne(`${mockApiUrl}/auth/change-password`);
      expect(req.request.method).toBe('POST');
      req.flush({ message: 'Password changed successfully' });
    });
  });

  describe('getProfile', () => {
    it('should fetch profile and update user signal', () => {
      service.getProfile().subscribe(user => {
        expect(user).toEqual(mockUser);
        expect(service.currentUser()).toEqual(mockUser);
      });

      const req = httpMock.expectOne(`${mockApiUrl}/auth/me`);
      expect(req.request.method).toBe('GET');
      req.flush(mockUser);
    });
  });

  describe('refresh', () => {
    it('should refresh tokens and update storage', () => {
      localStorage.setItem('refreshToken', 'old-refresh-token');
      const newTokenResponse: TokenResponse = {
        accessToken: 'new-access-token',
        refreshToken: 'new-refresh-token',
        emailVerified: true
      };

      service.refresh().subscribe(response => {
        expect(response).toEqual(newTokenResponse);
        expect(localStorage.getItem('accessToken')).toBe('new-access-token');
        expect(localStorage.getItem('refreshToken')).toBe('new-refresh-token');
      });

      const req = httpMock.expectOne(`${mockApiUrl}/auth/refresh`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ token: 'old-refresh-token' });
      req.flush(newTokenResponse);
    });

    it('should logout if no refresh token', () => {
      service.refresh().subscribe({
        next: () => fail('should have failed'),
        error: (error) => expect(error.message).toBe('No refresh token available.')
      });

      httpMock.expectNone(`${mockApiUrl}/auth/refresh`);
    });
  });

  describe('forgotPassword', () => {
    it('should send forgot password request', () => {
      service.forgotPassword('test@example.com').subscribe(response => {
        expect(response.message).toBe('Reset email sent');
      });

      const req = httpMock.expectOne(`${mockApiUrl}/auth/forgot-password`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ email: 'test@example.com' });
      req.flush({ message: 'Reset email sent' });
    });
  });

  describe('verifyEmail', () => {
    it('should verify email with token', () => {
      service.verifyEmail({ email: 'test@example.com', token: 'verify-token' }).subscribe(response => {
        expect(response.message).toBe('Email verified');
      });

      const req = httpMock.expectOne(`${mockApiUrl}/auth/verify-email`);
      expect(req.request.method).toBe('POST');
      req.flush({ message: 'Email verified' });
    });
  });

  describe('resendVerification', () => {
    it('should resend verification email', () => {
      service.resendVerification('test@example.com').subscribe(response => {
        expect(response.message).toBe('Verification email sent');
      });

      const req = httpMock.expectOne(`${mockApiUrl}/auth/resend-verification`);
      expect(req.request.method).toBe('POST');
      req.flush({ message: 'Verification email sent' });
    });
  });

  describe('validateTwoFactor', () => {
    it('should validate 2FA code and set user', () => {
      service.validateTwoFactor({ twoFactorToken: '2fa-token', code: '123456' }).subscribe(response => {
        expect(response).toEqual(mockTokenResponse);
        expect(service.isAuthenticated()).toBe(true);
      });

      const req = httpMock.expectOne(`${mockApiUrl}/auth/2fa/validate`);
      expect(req.request.method).toBe('POST');
      req.flush(mockTokenResponse);
    });
  });

  describe('setupTwoFactor', () => {
    it('should setup 2FA and return secret', () => {
      const mockSetup = { secret: 'SECRET123', otpAuthUri: 'otpauth://...' };

      service.setupTwoFactor().subscribe(response => {
        expect(response).toEqual(mockSetup);
      });

      const req = httpMock.expectOne(`${mockApiUrl}/auth/2fa/setup`);
      expect(req.request.method).toBe('POST');
      req.flush(mockSetup);
    });
  });

  describe('verifyTwoFactor', () => {
    it('should verify 2FA and return recovery codes', () => {
      const mockResponse = { recoveryCodes: ['code1', 'code2'] };

      service.verifyTwoFactor('123456').subscribe(response => {
        expect(response).toEqual(mockResponse);
      });

      const req = httpMock.expectOne(`${mockApiUrl}/auth/2fa/verify`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ code: '123456' });
      req.flush(mockResponse);
    });
  });

  describe('disableTwoFactor', () => {
    it('should disable 2FA', () => {
      service.disableTwoFactor({ password: 'pass', code: '123456' }).subscribe(response => {
        expect(response.message).toBe('2FA disabled');
      });

      const req = httpMock.expectOne(`${mockApiUrl}/auth/2fa/disable`);
      expect(req.request.method).toBe('POST');
      req.flush({ message: '2FA disabled' });
    });
  });

  describe('regenerateRecoveryCodes', () => {
    it('should regenerate recovery codes', () => {
      const mockResponse = { recoveryCodes: ['new1', 'new2'] };

      service.regenerateRecoveryCodes().subscribe(response => {
        expect(response).toEqual(mockResponse);
      });

      const req = httpMock.expectOne(`${mockApiUrl}/auth/2fa/recovery-codes`);
      expect(req.request.method).toBe('POST');
      req.flush(mockResponse);
    });
  });

  describe('resetPassword', () => {
    it('should reset password with token', () => {
      service.resetPassword({ email: 'test@example.com', token: 'reset-token', newPassword: 'NewPass123!' }).subscribe(response => {
        expect(response.message).toBe('Password reset');
      });

      const req = httpMock.expectOne(`${mockApiUrl}/auth/reset-password`);
      expect(req.request.method).toBe('POST');
      req.flush({ message: 'Password reset' });
    });
  });

  describe('address management', () => {
    it('should get addresses', () => {
      const mockAddresses = [{ id: 1, street: '123 Main St', city: 'City', state: 'State', postalCode: '12345', country: 'USA', isDefault: true, type: 'Shipping' }];

      service.getAddresses().subscribe(addresses => {
        expect(addresses).toEqual(mockAddresses);
      });

      const req = httpMock.expectOne(`${mockApiUrl}/auth/addresses`);
      expect(req.request.method).toBe('GET');
      req.flush(mockAddresses);
    });

    it('should add address', () => {
      const newAddress = { street: '456 Oak Ave', city: 'Town', state: 'State', postalCode: '67890', country: 'USA', isDefault: false, type: 'Billing' as const };
      const savedAddress = { id: 2, ...newAddress };

      service.addAddress(newAddress).subscribe(address => {
        expect(address).toEqual(savedAddress);
      });

      const req = httpMock.expectOne(`${mockApiUrl}/auth/addresses`);
      expect(req.request.method).toBe('POST');
      req.flush(savedAddress);
    });

    it('should update address', () => {
      const updateData = { street: '789 Pine Rd' };
      const updatedAddress = { id: 1, street: '789 Pine Rd', city: 'City', state: 'State', postalCode: '12345', country: 'USA', isDefault: true, type: 'Shipping' };

      service.updateAddress(1, updateData).subscribe(address => {
        expect(address).toEqual(updatedAddress);
      });

      const req = httpMock.expectOne(`${mockApiUrl}/auth/addresses/1`);
      expect(req.request.method).toBe('PUT');
      req.flush(updatedAddress);
    });

    it('should delete address', () => {
      service.deleteAddress(1).subscribe();

      const req = httpMock.expectOne(`${mockApiUrl}/auth/addresses/1`);
      expect(req.request.method).toBe('DELETE');
      req.flush(null);
    });
  });

  describe('role checks', () => {
    it('should return true for isAdmin when user is Admin', () => {
      const adminUser = { ...mockUser, role: 'Admin' };
      service['currentUserSignal'].set(adminUser);
      expect(service.isAdmin()).toBe(true);
    });

    it('should return false for isAdmin when user is Customer', () => {
      expect(service.isAdmin()).toBe(false);
    });

    it('should return true for isSubAdmin when user is SubAdmin', () => {
      const subAdminUser = { ...mockUser, role: 'SubAdmin' };
      service['currentUserSignal'].set(subAdminUser);
      expect(service.isSubAdmin()).toBe(true);
    });
  });

  describe('loadUserFromStorage', () => {
    it('should load user from localStorage on init', () => {
      localStorage.setItem('accessToken', 'token');
      localStorage.setItem('user', JSON.stringify(mockUser));

      const newService = TestBed.inject(AuthService);

      expect(newService.isAuthenticated()).toBe(true);
      expect(newService.currentUser()).toEqual(mockUser);
    });

    it('should logout if localStorage has invalid user data', () => {
      localStorage.setItem('accessToken', 'token');
      localStorage.setItem('user', 'invalid-json');

      const newService = TestBed.inject(AuthService);

      expect(newService.isAuthenticated()).toBe(false);
      expect(newService.currentUser()).toBeNull();
    });
  });
});