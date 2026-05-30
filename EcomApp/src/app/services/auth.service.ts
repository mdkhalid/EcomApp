import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { User, RegisterRequest, LoginRequest, TokenResponse, ChangePasswordRequest, Address, CreateAddressRequest, UpdateAddressRequest } from '../models/auth.model';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5068/api/auth';

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
        console.log('Loaded user from storage:', user);
      } catch {
        this.logout();
      }
    }
  }

  register(data: RegisterRequest): Observable<TokenResponse> {
    return this.http.post<TokenResponse>(`${this.apiUrl}/register`, data).pipe(
      tap(response => this.handleLoginSuccess(response))
    );
  }

  login(data: LoginRequest): Observable<TokenResponse> {
    return this.http.post<TokenResponse>(`${this.apiUrl}/login`, data).pipe(
      tap(response => this.handleLoginSuccess(response))
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
    return this.http.post<{ message: string }>(`${this.apiUrl}/change-password`, data);
  }

  getProfile(): Observable<User> {
    return this.http.get<User>(`${this.apiUrl}/me`).pipe(
      tap(user => {
        this.currentUserSignal.set(user);
        localStorage.setItem('user', JSON.stringify(user));
      })
    );
  }

  updateProfile(data: { firstName?: string; lastName?: string; phone?: string; gender?: string; dateOfBirth?: string }): Observable<User> {
    return this.http.put<User>(`${this.apiUrl}/profile`, data).pipe(
      tap(user => {
        this.currentUserSignal.set(user);
        localStorage.setItem('user', JSON.stringify(user));
      })
    );
  }

  uploadProfilePicture(file: File): Observable<User> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<User>(`${this.apiUrl}/profile/picture`, formData).pipe(
      tap(user => {
        this.currentUserSignal.set(user);
        localStorage.setItem('user', JSON.stringify(user));
      })
    );
  }

  removeProfilePicture(): Observable<User> {
    return this.http.delete<User>(`${this.apiUrl}/profile/picture`).pipe(
      tap(user => {
        this.currentUserSignal.set(user);
        localStorage.setItem('user', JSON.stringify(user));
      })
    );
  }

  getAddresses(): Observable<Address[]> {
    return this.http.get<Address[]>(`${this.apiUrl}/addresses`);
  }

  addAddress(data: CreateAddressRequest): Observable<Address> {
    return this.http.post<Address>(`${this.apiUrl}/addresses`, data);
  }

  updateAddress(id: number, data: UpdateAddressRequest): Observable<Address> {
    return this.http.put<Address>(`${this.apiUrl}/addresses/${id}`, data);
  }

  deleteAddress(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/addresses/${id}`);
  }

  isAdmin(): boolean {
    return this.currentUserSignal()?.role === 'Admin';
  }

  private handleLoginSuccess(response: TokenResponse): void {
    localStorage.setItem('accessToken', response.accessToken);
    localStorage.setItem('refreshToken', response.refreshToken);
    const tokenPayload = this.decodeToken(response.accessToken);
    console.log('JWT Token Payload:', tokenPayload);
    const role = tokenPayload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
      || tokenPayload['role']
      || 'Customer';
    console.log('Extracted role:', role);
    const user: User = {
      id: parseInt(tokenPayload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier']),
      email: tokenPayload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'],
      username: tokenPayload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'],
      role: role,
      firstName: tokenPayload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname'],
      lastName: tokenPayload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname'],
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
