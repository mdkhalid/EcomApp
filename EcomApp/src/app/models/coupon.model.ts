export interface Coupon {
  id: number;
  code: string;
  description: string;
  type: string;
  value: number;
  minCartValue: number;
  maxUses: number;
  currentUses: number;
  expiresAt: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateCoupon {
  code: string;
  description: string;
  type: string;
  value: number;
  minCartValue: number;
  maxUses: number;
  expiresAt: string;
  isActive: boolean;
}

export interface ValidateCouponRequest {
  code: string;
  cartTotal: number;
}

export interface ValidateCouponResponse {
  isValid: boolean;
  errorMessage?: string;
  code?: string;
  description?: string;
  type?: string;
  discountAmount: number;
  finalTotal: number;
}
