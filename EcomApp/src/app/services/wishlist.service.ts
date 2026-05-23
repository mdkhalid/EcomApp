import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { WishlistItem, WishlistResponse } from '../models/wishlist.model';

@Injectable({
  providedIn: 'root'
})
export class WishlistService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5068/api/wishlist';

  private wishlistIds = signal<Set<number>>(new Set());

  getWishlist(): Observable<WishlistResponse> {
    return this.http.get<WishlistResponse>(this.apiUrl).pipe(
      tap(res => this.wishlistIds.set(new Set(res.items.map(i => i.productId))))
    );
  }

  check(productId: number): Observable<{ isWishlisted: boolean }> {
    return this.http.get<{ isWishlisted: boolean }>(`${this.apiUrl}/check/${productId}`);
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
      })
    );
  }

  remove(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
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
