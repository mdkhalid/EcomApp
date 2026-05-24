export interface User {
  id: number;
  email: string;
  username: string;
  role: string;
  firstName?: string;
  lastName?: string;
  phone?: string;
  isActive?: boolean;
  createdAt: string;
  lastLoginAt?: string;
  createdBy?: string;
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
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
  confirmNewPassword: string;
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
