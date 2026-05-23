export interface Review {
  id: number;
  productId: number;
  userId: number;
  userName: string;
  rating: number;
  comment: string;
  createdAt: string;
}

export interface CreateReview {
  rating: number;
  comment: string;
}

export interface ProductRatingInfo {
  averageRating: number;
  totalReviews: number;
}

export interface ReviewResponse {
  items: Review[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  averageRating: number;
  totalReviews: number;
}
