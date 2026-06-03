export interface Cart {
  id: number;
  items: CartItem[];
  totalAmount: number;
  createdAt: string;
  updatedAt: string;
}

export interface CartItem {
  id: number;
  productId: number;
  productName: string;
  productImage: string;
  productVariantId?: number;
  variantName?: string;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
}

export interface AddCartItem {
  productId: number;
  quantity: number;
  productVariantId?: number;
}

export interface UpdateCartItem {
  quantity: number;
}
