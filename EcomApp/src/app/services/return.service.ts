import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { map, Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { API_URL } from '../utils/api-config';

import { ReturnRequest, CreateReturnRequest } from '../models/return.model';

@Injectable({
  providedIn: 'root'
})
export class ReturnService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = API_URL + '/returns';

  createReturnRequest(dto: CreateReturnRequest): Observable<ReturnRequest> {
    return this.http.post<ReturnRequest>(this.apiUrl, dto).pipe(
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  getMyReturns(): Observable<ReturnRequest[]> {
    return this.http.get<ReturnRequest[]>(`${this.apiUrl}/my-returns`).pipe(
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  getById(id: number): Observable<ReturnRequest> {
    return this.http.get<ReturnRequest>(`${this.apiUrl}/${id}`).pipe(
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  getByOrder(orderId: number): Observable<ReturnRequest | null> {
    return this.http.get<ReturnRequest | null>(`${this.apiUrl}/order/${orderId}`).pipe(
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  getAll(pageNumber = 1, pageSize = 20, status?: string): Observable<{ items: ReturnRequest[]; totalCount: number; pageNumber: number; pageSize: number }> {
    let url = `${this.apiUrl}?pageNumber=${pageNumber}&pageSize=${pageSize}`;
    if (status) url += `&status=${status}`;
    return this.http.get<{ items: ReturnRequest[]; totalCount: number; pageNumber: number; pageSize: number }>(url).pipe(
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  updateStatus(id: number, status: string, adminNote?: string): Observable<ReturnRequest> {
    return this.http.put<ReturnRequest>(`${this.apiUrl}/${id}/status`, { status, adminNote }).pipe(
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }
}
