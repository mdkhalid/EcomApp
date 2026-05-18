import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Cart, AddCartItem, UpdateCartItem } from '../models/cart.model';

@Injectable({
  providedIn: 'root'
})
export class CartService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5068/api/carts';

  getCart(): Observable<Cart> {
    return this.http.get<Cart>(this.apiUrl);
  }

  addItem(item: AddCartItem): Observable<Cart> {
    return this.http.post<Cart>(`${this.apiUrl}/items`, item);
  }

  updateItem(cartItemId: number, item: UpdateCartItem): Observable<Cart> {
    return this.http.put<Cart>(`${this.apiUrl}/items/${cartItemId}`, item);
  }

  removeItem(cartItemId: number): Observable<Cart> {
    return this.http.delete<Cart>(`${this.apiUrl}/items/${cartItemId}`);
  }

  clearCart(): Observable<Cart> {
    return this.http.delete<Cart>(this.apiUrl);
  }
}
