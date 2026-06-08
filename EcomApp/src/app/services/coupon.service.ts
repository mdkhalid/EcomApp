import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { API_URL } from '../utils/api-config';

import { Coupon, CreateCoupon, ValidateCouponRequest, ValidateCouponResponse } from '../models/coupon.model';

@Injectable({ providedIn: 'root' })
export class CouponService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = API_URL + '/coupons';

  getAll(): Observable<Coupon[]> {
    return this.http.get<Coupon[]>(this.apiUrl).pipe(
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  getById(id: number): Observable<Coupon> {
    return this.http.get<Coupon>(`${this.apiUrl}/${id}`).pipe(
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  create(coupon: CreateCoupon): Observable<Coupon> {
    return this.http.post<Coupon>(this.apiUrl, coupon).pipe(
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  update(id: number, coupon: CreateCoupon): Observable<Coupon> {
    return this.http.put<Coupon>(`${this.apiUrl}/${id}`, coupon).pipe(
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`).pipe(
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  validate(request: ValidateCouponRequest): Observable<ValidateCouponResponse> {
    return this.http.post<ValidateCouponResponse>(`${this.apiUrl}/validate`, request).pipe(
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }
}
