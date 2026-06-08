import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { API_URL } from '../utils/api-config';

import { WishlistItem, WishlistResponse } from '../models/wishlist.model';

@Injectable({
  providedIn: 'root'
})
export class WishlistService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = API_URL + '/wishlist';

  private wishlistIds = signal<Set<number>>(new Set());

  getWishlist(): Observable<WishlistResponse> {
    return this.http.get<WishlistResponse>(this.apiUrl).pipe(
      tap(res => this.wishlistIds.set(new Set(res.items.map(i => i.productId)))),
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  check(productId: number): Observable<{ isWishlisted: boolean }> {
    return this.http.get<{ isWishlisted: boolean }>(`${this.apiUrl}/check/${productId}`).pipe(
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  toggle(productId: number): Observable<{ wishlisted: boolean; message: string }> {
    return this.http.post<{ wishlisted: boolean; message: string }>(`${this.apiUrl}/products/${productId}`, {}).pipe(
      tap(res => {
        const ids = this.wishlistIds();
        if (res.wishlisted) {
          ids.add(productId);
        } else {
          ids.delete(productId);
        }
        this.wishlistIds.set(new Set(ids));
      }),
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  remove(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`).pipe(
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  isWishlisted(productId: number): boolean {
    return this.wishlistIds().has(productId);
  }

  loadWishlistIds(): void {
    this.getWishlist().subscribe({
      error: () => { /* silently fail - will retry on next page load */ }
    });
  }
}
