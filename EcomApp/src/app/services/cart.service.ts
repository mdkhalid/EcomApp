import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { API_URL } from '../utils/api-config';

import { Cart, AddCartItem, UpdateCartItem } from '../models/cart.model';

@Injectable({
  providedIn: 'root'
})
export class CartService {
  readonly cartItemCount = signal(0);
  private readonly http = inject(HttpClient);
  private readonly apiUrl = API_URL + '/carts';

  private updateCountFromCart(cart: Cart): Cart {
    this.cartItemCount.set(cart.items.reduce((sum, item) => sum + item.quantity, 0));
    return cart;
  }

  /** Clears the count signal (e.g., on logout) */
  resetCount(): void {
    this.cartItemCount.set(0);
  }

  getCart(): Observable<Cart> {
    return this.http.get<Cart>(this.apiUrl).pipe(
      tap(cart => this.updateCountFromCart(cart)),
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  addItem(item: AddCartItem): Observable<Cart> {
    return this.http.post<Cart>(`${this.apiUrl}/items`, item).pipe(
      tap(cart => this.updateCountFromCart(cart)),
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  updateItem(cartItemId: number, item: UpdateCartItem): Observable<Cart> {
    return this.http.put<Cart>(`${this.apiUrl}/items/${cartItemId}`, item).pipe(
      tap(cart => this.updateCountFromCart(cart)),
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  removeItem(cartItemId: number): Observable<Cart> {
    return this.http.delete<Cart>(`${this.apiUrl}/items/${cartItemId}`).pipe(
      tap(cart => this.updateCountFromCart(cart)),
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  clearCart(): Observable<Cart> {
    return this.http.delete<Cart>(this.apiUrl).pipe(
      tap(cart => this.updateCountFromCart(cart)),
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  mergeCart(): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/merge`, {}).pipe(
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }
}
