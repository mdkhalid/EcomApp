export interface ReturnRequest {
  id: number;
  orderId: number;
  userId: number;
  userName: string;
  reason: string;
  comment?: string;
  status: string;
  adminNote?: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateReturnRequest {
  orderId: number;
  reason: string;
  comment?: string;
}
