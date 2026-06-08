import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { API_URL } from '../utils/api-config';

import { Order, CreateOrder, SavedAddress, OrderTracking } from '../models/order.model';

@Injectable({
  providedIn: 'root'
})
export class OrderService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = API_URL + '/orders';

  createOrder(order: CreateOrder): Observable<Order> {
    return this.http.post<Order>(this.apiUrl, order);
  }

  getOrders(): Observable<Order[]> {
    return this.http.get<{ items: Order[] }>(this.apiUrl).pipe(
      map(res => res.items)
    );
  }

  getById(id: number): Observable<Order> {
    return this.http.get<Order>(`${this.apiUrl}/${id}`);
  }

  getTracking(id: number): Observable<OrderTracking> {
    return this.http.get<OrderTracking>(`${this.apiUrl}/${id}/tracking`);
  }

  getPreviousAddresses(): Observable<SavedAddress[]> {
    return this.http.get<SavedAddress[]>(`${this.apiUrl}/previous-addresses`);
  }
}
