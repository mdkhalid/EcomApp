export interface User {
  id: number;
  email: string;
  username: string;
  role: string;
  firstName?: string;
  lastName?: string;
  phone?: string;
  profilePictureUrl?: string;
  gender?: string;
  dateOfBirth?: string;
  isActive?: boolean;
  createdAt: string;
  lastLoginAt?: string;
  createdBy?: string;
  failedLoginAttempts?: number;
  lockoutEnd?: string;
  lockoutReason?: string;
  isLockedOut?: boolean;
  emailVerified?: boolean;
  twoFactorEnabled?: boolean;
  addresses?: Address[];
}

export interface Address {
  id: number;
  label: string;
  street: string;
  city: string;
  state: string;
  zipCode: string;
  country: string;
  isDefault: boolean;
}

export interface CreateAddressRequest {
  label: string;
  street: string;
  city: string;
  state: string;
  zipCode: string;
  country: string;
  isDefault: boolean;
}

export interface UpdateAddressRequest {
  label?: string;
  street?: string;
  city?: string;
  state?: string;
  zipCode?: string;
  country?: string;
  isDefault?: boolean;
}

export interface RegisterRequest {
  email: string;
  username: string;
  password: string;
  confirmPassword: string;
  firstName?: string;
  lastName?: string;
  phone?: string;
}

export interface LoginRequest {
  emailOrUsername: string;
  password: string;
}

export interface TokenResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  tokenType: string;
  emailVerified: boolean;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
  confirmNewPassword: string;
}

export interface TwoFactorChallenge {
  requiresTwoFactor: boolean;
  twoFactorToken: string;
}

export interface CreateUserRequest {
  email: string;
  username: string;
  password: string;
  confirmPassword: string;
  role: string;
  firstName?: string;
  lastName?: string;
  phone?: string;
}

export interface AdminChangePasswordRequest {
  newPassword: string;
  confirmPassword: string;
}
