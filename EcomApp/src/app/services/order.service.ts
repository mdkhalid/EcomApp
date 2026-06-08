import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { map, Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { API_URL } from '../utils/api-config';

import { Order, CreateOrder, SavedAddress, OrderTracking } from '../models/order.model';

@Injectable({
  providedIn: 'root'
})
export class OrderService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = API_URL + '/orders';

  createOrder(order: CreateOrder): Observable<Order> {
    return this.http.post<Order>(this.apiUrl, order).pipe(
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  getOrders(): Observable<Order[]> {
    return this.http.get<{ items: Order[] }>(this.apiUrl).pipe(
      map(res => res.items),
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  getById(id: number): Observable<Order> {
    return this.http.get<Order>(`${this.apiUrl}/${id}`).pipe(
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  getTracking(id: number): Observable<OrderTracking> {
    return this.http.get<OrderTracking>(`${this.apiUrl}/${id}/tracking`).pipe(
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  getPreviousAddresses(): Observable<SavedAddress[]> {
    return this.http.get<SavedAddress[]>(`${this.apiUrl}/previous-addresses`).pipe(
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }
}
