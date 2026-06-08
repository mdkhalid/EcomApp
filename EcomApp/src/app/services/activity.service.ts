import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { RecentlyViewedProduct } from '../models/activity.model';
import { API_URL } from '../utils/api-config';

@Injectable({
  providedIn: 'root'
})
export class ActivityService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = API_URL;

  logActivity(type: string, data: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/activity`, { type, data }).pipe(
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  getRecentlyViewed(): Observable<RecentlyViewedProduct[]> {
    return this.http.get<RecentlyViewedProduct[]>(`${this.apiUrl}/recommendations/recently-viewed`).pipe(
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  getForYou(): Observable<RecentlyViewedProduct[]> {
    return this.http.get<RecentlyViewedProduct[]>(`${this.apiUrl}/recommendations/for-you`).pipe(
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  getTrending(): Observable<RecentlyViewedProduct[]> {
    return this.http.get<RecentlyViewedProduct[]>(`${this.apiUrl}/recommendations/trending`).pipe(
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  getAlsoBought(productId: number): Observable<RecentlyViewedProduct[]> {
    return this.http.get<RecentlyViewedProduct[]>(`${this.apiUrl}/recommendations/also-bought/${productId}`).pipe(
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  getFrequentlyBoughtTogether(productId: number): Observable<RecentlyViewedProduct[]> {
    return this.http.get<RecentlyViewedProduct[]>(`${this.apiUrl}/recommendations/frequently-bought-together/${productId}`).pipe(
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }
}
