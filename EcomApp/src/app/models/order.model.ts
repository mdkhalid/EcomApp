export interface Order {
  id: number;
  status: string;
  shippingName: string;
  shippingAddress: string;
  shippingCity: string;
  shippingZip: string;
  totalAmount: number;
  items: OrderItem[];
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

export interface CreateOrder {
  shippingName: string;
  shippingAddress: string;
  shippingCity: string;
  shippingZip: string;
}

export interface SavedAddress {
  name: string;
  address: string;
  city: string;
  zip: string;
}
