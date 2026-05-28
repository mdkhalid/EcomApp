import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Review, CreateReview, ReviewResponse } from '../models/review.model';

@Injectable({
  providedIn: 'root'
})
export class ReviewService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5068/api/reviews';

  getByProduct(productId: number, pageNumber = 1, pageSize = 20): Observable<ReviewResponse> {
    return this.http.get<ReviewResponse>(`${this.apiUrl}/product/${productId}`, {
      params: { pageNumber: pageNumber.toString(), pageSize: pageSize.toString() }
    });
  }

  canReview(productId: number): Observable<{ canReview: boolean; reason: string }> {
    return this.http.get<{ canReview: boolean; reason: string }>(`${this.apiUrl}/product/${productId}/can-review`);
  }

  create(productId: number, review: CreateReview): Observable<Review> {
    return this.http.post<Review>(`${this.apiUrl}/product/${productId}`, review);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
