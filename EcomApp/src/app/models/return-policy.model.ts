export interface ReturnPolicy {
  returnWindowDays: number;
  isActive: boolean;
  policyText: string;
  updatedAt: string;
  updatedBy?: string;
}

export interface UpdateReturnPolicy {
  returnWindowDays: number;
  isActive: boolean;
  policyText: string;
}
