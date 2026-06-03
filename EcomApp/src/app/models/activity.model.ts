export interface RecentlyViewedProduct {
  productId: number;
  name: string;
  imageUrl?: string;
  price: number;
  originalPrice?: number;
  discountPercent: number;
  category: string;
  averageRating: number;
  totalReviews: number;
}
