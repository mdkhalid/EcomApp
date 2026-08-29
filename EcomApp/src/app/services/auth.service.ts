import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { API_URL } from '../utils/api-config';

import { User, RegisterRequest, LoginRequest, TokenResponse, ChangePasswordRequest, Address, CreateAddressRequest, UpdateAddressRequest } from '../models/auth.model';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = API_URL + '/auth';

  private currentUserSignal = signal<User | null>(null);
  private isAuthenticatedSignal = signal<boolean>(false);

  currentUser = this.currentUserSignal.asReadonly();
  isAuthenticated = this.isAuthenticatedSignal.asReadonly();

  constructor() {
    this.loadUserFromStorage();
  }

  private loadUserFromStorage(): void {
    const token = localStorage.getItem('accessToken');
    const userStr = localStorage.getItem('user');
    if (token && userStr) {
      try {
        const user = JSON.parse(userStr) as User;
        this.currentUserSignal.set(user);
        this.isAuthenticatedSignal.set(true);
      } catch {
        this.logout();
      }
    }
  }

  register(data: RegisterRequest): Observable<TokenResponse> {
    return this.http.post<TokenResponse>(`${this.apiUrl}/register`, data).pipe(
      tap(response => this.handleLoginSuccess(response)),
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  login(data: LoginRequest): Observable<TokenResponse | TwoFactorChallenge> {
    return this.http.post<TokenResponse | TwoFactorChallenge>(`${this.apiUrl}/login`, data).pipe(
      tap(response => {
        const challenge = response as Partial<TwoFactorChallenge>;
        if (challenge.requiresTwoFactor) return;
        this.handleLoginSuccess(response as TokenResponse);
      }),
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  logout(): void {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('user');
    this.currentUserSignal.set(null);
    this.isAuthenticatedSignal.set(false);
  }

  changePassword(data: ChangePasswordRequest): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/change-password`, data).pipe(
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  getProfile(): Observable<User> {
    return this.http.get<User>(`${this.apiUrl}/me`).pipe(
      tap(user => {
        this.currentUserSignal.set(user);
        localStorage.setItem('user', JSON.stringify(user));
      }),
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  updateProfile(data: { firstName?: string; lastName?: string; phone?: string; gender?: string; dateOfBirth?: string }): Observable<User> {
    return this.http.put<User>(`${this.apiUrl}/profile`, data).pipe(
      tap(user => {
        this.currentUserSignal.set(user);
        localStorage.setItem('user', JSON.stringify(user));
      }),
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  uploadProfilePicture(file: File): Observable<User> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<User>(`${this.apiUrl}/profile/picture`, formData).pipe(
      tap(user => {
        this.currentUserSignal.set(user);
        localStorage.setItem('user', JSON.stringify(user));
      }),
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  removeProfilePicture(): Observable<User> {
    return this.http.delete<User>(`${this.apiUrl}/profile/picture`).pipe(
      tap(user => {
        this.currentUserSignal.set(user);
        localStorage.setItem('user', JSON.stringify(user));
      }),
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  getAddresses(): Observable<Address[]> {
    return this.http.get<Address[]>(`${this.apiUrl}/addresses`).pipe(
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  /** Call the backend refresh endpoint and rotate stored tokens.
   *  Returns the new TokenResponse so the interceptor can replay the original request. */
  refresh(): Observable<TokenResponse> {
    const token = localStorage.getItem('refreshToken');
    if (!token) {
      this.logout();
      return throwError(() => new Error('No refresh token available.'));
    }
    return this.http.post<TokenResponse>(`${this.apiUrl}/refresh`, { token }).pipe(
      tap(response => {
        localStorage.setItem('accessToken', response.accessToken);
        localStorage.setItem('refreshToken', response.refreshToken);
      }),
      catchError(err => {
        // Refresh itself failed (expired / revoked) → force logout
        this.logout();
        return throwError(() => err);
      })
    );
  }

  forgotPassword(email: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/forgot-password`, { email }).pipe(
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  verifyEmail(data: { email: string; token: string }): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/verify-email`, data).pipe(
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  resendVerification(email: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/resend-verification`, { email }).pipe(
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  validateTwoFactor(data: { twoFactorToken: string; code: string }): Observable<TokenResponse> {
    return this.http.post<TokenResponse>(`${this.apiUrl}/2fa/validate`, data).pipe(
      tap(response => this.handleLoginSuccess(response)),
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  setupTwoFactor(): Observable<{ secret: string; otpAuthUri: string }> {
    return this.http.post<{ secret: string; otpAuthUri: string }>(`${this.apiUrl}/2fa/setup`, {}).pipe(
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  verifyTwoFactor(code: string): Observable<{ recoveryCodes: string[] }> {
    return this.http.post<{ recoveryCodes: string[] }>(`${this.apiUrl}/2fa/verify`, { code }).pipe(
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  disableTwoFactor(data: { password: string; code: string }): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/2fa/disable`, data).pipe(
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  regenerateRecoveryCodes(): Observable<{ recoveryCodes: string[] }> {
    return this.http.post<{ recoveryCodes: string[] }>(`${this.apiUrl}/2fa/recovery-codes`, {}).pipe(
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  resetPassword(data: { email: string; token: string; newPassword: string }): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/reset-password`, data).pipe(
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  addAddress(data: CreateAddressRequest): Observable<Address> {
    return this.http.post<Address>(`${this.apiUrl}/addresses`, data).pipe(
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  updateAddress(id: number, data: UpdateAddressRequest): Observable<Address> {
    return this.http.put<Address>(`${this.apiUrl}/addresses/${id}`, data).pipe(
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  deleteAddress(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/addresses/${id}`).pipe(
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  isAdmin(): boolean {
    return this.currentUserSignal()?.role === 'Admin';
  }

  isSuperAdmin(): boolean {
    return this.currentUserSignal()?.role === 'Admin';
  }

  isSubAdmin(): boolean {
    return this.currentUserSignal()?.role === 'SubAdmin';
  }

  private handleLoginSuccess(response: TokenResponse): void {
    localStorage.setItem('accessToken', response.accessToken);
    localStorage.setItem('refreshToken', response.refreshToken);
    const tokenPayload = this.decodeToken(response.accessToken);
    const role = tokenPayload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
      || tokenPayload['role']
      || 'Customer';
    const user: User = {
      id: parseInt(tokenPayload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier']),
      email: tokenPayload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'],
      username: tokenPayload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'],
      role: role,
      firstName: tokenPayload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname'],
      lastName: tokenPayload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname'],
      emailVerified: response.emailVerified,
      createdAt: new Date().toISOString()
    };
    this.currentUserSignal.set(user);
    this.isAuthenticatedSignal.set(true);
    localStorage.setItem('user', JSON.stringify(user));
  }

  private decodeToken(token: string): Record<string, string> {
    const parts = token.split('.');
    const payload = JSON.parse(atob(parts[1]));
    return payload;
  }
}
