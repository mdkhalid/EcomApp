import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { ReturnRequest, CreateReturnRequest } from '../models/return.model';

@Injectable({
  providedIn: 'root'
})
export class ReturnService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5068/api/returns';

  createReturnRequest(dto: CreateReturnRequest): Observable<ReturnRequest> {
    return this.http.post<ReturnRequest>(this.apiUrl, dto);
  }

  getMyReturns(): Observable<ReturnRequest[]> {
    return this.http.get<ReturnRequest[]>(`${this.apiUrl}/my-returns`);
  }

  getById(id: number): Observable<ReturnRequest> {
    return this.http.get<ReturnRequest>(`${this.apiUrl}/${id}`);
  }

  getByOrder(orderId: number): Observable<ReturnRequest | null> {
    return this.http.get<ReturnRequest | null>(`${this.apiUrl}/order/${orderId}`);
  }

  getAll(pageNumber = 1, pageSize = 20, status?: string): Observable<{ items: ReturnRequest[]; totalCount: number; pageNumber: number; pageSize: number }> {
    let url = `${this.apiUrl}?pageNumber=${pageNumber}&pageSize=${pageSize}`;
    if (status) url += `&status=${status}`;
    return this.http.get<{ items: ReturnRequest[]; totalCount: number; pageNumber: number; pageSize: number }>(url);
  }

  updateStatus(id: number, status: string, adminNote?: string): Observable<ReturnRequest> {
    return this.http.put<ReturnRequest>(`${this.apiUrl}/${id}/status`, { status, adminNote });
  }
}
