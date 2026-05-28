export interface Order {
  id: number;
  status: string;
  shippingName: string;
  shippingAddress: string;
  shippingCity: string;
  shippingZip: string;
  totalAmount: number;
  trackingNumber?: string;
  carrier?: string;
  estimatedDeliveryDate?: string;
  actualDeliveryDate?: string;
  customerEmail?: string;
  customerPhone?: string;
  items: OrderItem[];
  statusHistory: OrderStatusHistory[];
  createdAt: string;
  updatedAt: string;
}

export interface OrderItem {
  id: number;
  productId: number;
  productName: string;
  productImage: string;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
}

export interface OrderStatusHistory {
  id: number;
  status: string;
  note?: string;
  location?: string;
  createdAt: string;
}

export interface CreateOrder {
  shippingName: string;
  shippingAddress: string;
  shippingCity: string;
  shippingZip: string;
  customerEmail?: string;
  customerPhone?: string;
}

export interface SavedAddress {
  name: string;
  address: string;
  city: string;
  zip: string;
}

export interface OrderTracking {
  orderId: number;
  status: string;
  trackingNumber?: string;
  carrier?: string;
  estimatedDeliveryDate?: string;
  actualDeliveryDate?: string;
  statusHistory: OrderStatusHistory[];
}
