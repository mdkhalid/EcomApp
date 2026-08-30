import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_URL } from '../utils/api-config';

export interface CreateStockAlertDto {
  productId: number;
  variantId?: number;
}

export interface StockAlertDto {
  id: number;
  productId: number;
  variantId?: number;
  createdAt: string;
  notifiedAt?: string;
}

@Injectable({
  providedIn: 'root'
})
export class StockAlertService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = API_URL + '/products';

  createAlert(dto: CreateStockAlertDto): Observable<StockAlertDto> {
    return this.http.post<StockAlertDto>(`${this.apiUrl}/${dto.productId}/stock-alerts`, dto);
  }

  deleteAlert(productId: number, variantId?: number): Observable<void> {
    let params = new HttpParams();
    if (variantId !== undefined) {
      params = params.set('variantId', variantId.toString());
    }
    return this.http.delete<void>(`${this.apiUrl}/${productId}/stock-alerts`, { params });
  }

  getMyAlerts(productId: number): Observable<StockAlertDto[]> {
    return this.http.get<StockAlertDto[]>(`${this.apiUrl}/${productId}/stock-alerts`);
  }
}