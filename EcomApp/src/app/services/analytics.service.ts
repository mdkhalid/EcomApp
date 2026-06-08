import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_URL } from '../utils/api-config';

import {
  AnalyticsOverview,
  CategoryBreakdown,
  LowStockProduct,
  OrderStatusBreakdown,
  PageViewSummary,
  RevenueSummary,
  TopPage,
  TopProduct,
  TopSearch
} from '../models/analytics.model';

@Injectable({
  providedIn: 'root'
})
export class AnalyticsService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = API_URL + '/analytics';

  getOverview(): Observable<AnalyticsOverview> {
    return this.http.get<AnalyticsOverview>(`${this.apiUrl}/overview`);
  }

  getRevenue(period: 'daily' | 'weekly' | 'monthly' = 'monthly'): Observable<RevenueSummary> {
    const params = new HttpParams().set('period', period);
    return this.http.get<RevenueSummary>(`${this.apiUrl}/revenue`, { params });
  }

  getTopProducts(limit = 10): Observable<TopProduct[]> {
    const params = new HttpParams().set('limit', String(limit));
    return this.http.get<TopProduct[]>(`${this.apiUrl}/top-products`, { params });
  }

  getCategoryBreakdown(): Observable<CategoryBreakdown[]> {
    return this.http.get<CategoryBreakdown[]>(`${this.apiUrl}/category-breakdown`);
  }

  getOrderStatusBreakdown(): Observable<OrderStatusBreakdown[]> {
    return this.http.get<OrderStatusBreakdown[]>(`${this.apiUrl}/order-status`);
  }

  getLowStock(threshold = 10): Observable<LowStockProduct[]> {
    const params = new HttpParams().set('threshold', String(threshold));
    return this.http.get<LowStockProduct[]>(`${this.apiUrl}/low-stock`, { params });
  }

  getPageViews(period = '7d'): Observable<PageViewSummary> {
    const params = new HttpParams().set('period', period);
    return this.http.get<PageViewSummary>(`${this.apiUrl}/page-views`, { params });
  }

  getTopPages(period = '7d'): Observable<TopPage[]> {
    const params = new HttpParams().set('period', period);
    return this.http.get<TopPage[]>(`${this.apiUrl}/top-pages`, { params });
  }

  getTopSearches(period = '7d'): Observable<TopSearch[]> {
    const params = new HttpParams().set('period', period);
    return this.http.get<TopSearch[]>(`${this.apiUrl}/top-searches`, { params });
  }
}
