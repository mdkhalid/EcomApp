export interface RevenuePoint {
  label: string;
  date: string;
  revenue: number;
  orderCount: number;
}

export interface RevenueSummary {
  totalRevenue: number;
  totalOrders: number;
  averageOrderValue: number;
  period: 'daily' | 'weekly' | 'monthly';
  points: RevenuePoint[];
}

export interface TopProduct {
  productId: number;
  productName: string;
  imageUrl?: string;
  category?: string;
  unitsSold: number;
  revenue: number;
}

export interface CategoryBreakdown {
  category: string;
  orderCount: number;
  unitsSold: number;
  revenue: number;
}

export interface OrderStatusBreakdown {
  status: string;
  count: number;
}

export interface LowStockProduct {
  productId: number;
  productName: string;
  category?: string;
  imageUrl?: string;
  stock: number;
}

export interface PageViewPoint {
  date: string;
  label: string;
  count: number;
}

export interface PageViewSummary {
  period: string;
  totalViews: number;
  uniqueVisitors: number;
  views: PageViewPoint[];
  uniqueVisitorsPoints: PageViewPoint[];
}

export interface TopPage {
  path: string;
  count: number;
}

export interface TopSearch {
  keyword: string;
  count: number;
}

export interface AnalyticsOverview {
  totalProducts: number;
  totalOrders: number;
  totalUsers: number;
  pendingOrders: number;
  orderStatusBreakdown: OrderStatusBreakdown[];
  topProducts: TopProduct[];
  lowStockProducts: LowStockProduct[];
  pageViewsToday: number;
  uniqueVisitorsToday: number;
}
