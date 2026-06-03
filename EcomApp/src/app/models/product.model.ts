export interface Product {
  id: number;
  name: string;
  description: string;
  price: number;
  originalPrice?: number;
  stock: number;
  category: string;
  brand?: string;
  imageUrl?: string;
  isActive: boolean;
  averageRating: number;
  totalReviews: number;
  discountPercent: number;
  images: ProductImage[];
}

export interface ProductImage {
  id: number;
  imageUrl: string;
  sortOrder: number;
}

export interface CreateProduct {
  name: string;
  description: string;
  price: number;
  originalPrice?: number;
  stock: number;
  category: string;
  brand?: string;
}

export interface UpdateProduct {
  name: string;
  description: string;
  price: number;
  originalPrice?: number;
  stock: number;
  category: string;
  brand?: string;
  imageUrl?: string;
}

export interface SearchFilter {
  search?: string;
  category?: string;
  brand?: string;
  minPrice?: number;
  maxPrice?: number;
  minRating?: number;
  minDiscount?: number;
  inStock?: boolean;
  sortBy?: string;
  pageNumber: number;
  pageSize: number;
}

export interface SearchResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  filters: FilterMetadata;
}

export interface FilterMetadata {
  categories: string[];
  brands: string[];
  minPrice: number;
  maxPrice: number;
  priceStep: number;
  ratingBuckets: RatingBucket[];
  discountBuckets: DiscountBucket[];
}

export interface RatingBucket {
  minRating: number;
  maxRating: number;
  count: number;
  label: string;
}

export interface DiscountBucket {
  minDiscount: number;
  maxDiscount: number;
  count: number;
  label: string;
}

export interface SearchSuggestion {
  suggestions: string[];
  recentSearches: string[];
  popularCategories: string[];
}

export interface PriceRange {
  min: number;
  max: number;
  step: number;
}
