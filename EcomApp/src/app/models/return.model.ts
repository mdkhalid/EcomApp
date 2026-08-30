export interface ReturnRequest {
  id: number;
  orderId: number;
  userId: number;
  userName: string;
  productId: number;
  productName: string;
  quantity: number;
  reason: string;
  comment?: string;
  status: string;
  adminNote?: string;
  createdAt: string;
  updatedAt: string;
  requestedAt?: string;
}

export interface CreateReturnRequest {
  orderId: number;
  reason: string;
  comment?: string;
}
