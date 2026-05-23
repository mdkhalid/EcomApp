export interface WishlistItem {
  id: number;
  productId: number;
  productName: string;
  productImage?: string;
  productPrice: number;
  category: string;
  createdAt: string;
}

export interface WishlistResponse {
  items: WishlistItem[];
  totalCount: number;
}
