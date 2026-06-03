import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { RecentlyViewedProduct } from '../models/activity.model';

@Injectable({
  providedIn: 'root'
})
export class ActivityService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5068/api';

  logActivity(type: string, data: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/activity`, { type, data });
  }

  getRecentlyViewed(): Observable<RecentlyViewedProduct[]> {
    return this.http.get<RecentlyViewedProduct[]>(`${this.apiUrl}/recommendations/recently-viewed`);
  }

  getForYou(): Observable<RecentlyViewedProduct[]> {
    return this.http.get<RecentlyViewedProduct[]>(`${this.apiUrl}/recommendations/for-you`);
  }

  getTrending(): Observable<RecentlyViewedProduct[]> {
    return this.http.get<RecentlyViewedProduct[]>(`${this.apiUrl}/recommendations/trending`);
  }
}
