import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { API_URL } from '../utils/api-config';

import { Banner, CreateBanner, UpdateBanner } from '../models/banner.model';

@Injectable({
  providedIn: 'root'
})
export class BannerService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = API_URL + '/banners';

  getAll(): Observable<Banner[]> {
    return this.http.get<Banner[]>(this.apiUrl).pipe(
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  getActive(): Observable<Banner[]> {
    return this.http.get<Banner[]>(`${this.apiUrl}/active`).pipe(
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  getById(id: number): Observable<Banner> {
    return this.http.get<Banner>(`${this.apiUrl}/${id}`).pipe(
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  create(banner: CreateBanner): Observable<Banner> {
    return this.http.post<Banner>(this.apiUrl, banner).pipe(
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  update(id: number, banner: UpdateBanner): Observable<Banner> {
    return this.http.put<Banner>(`${this.apiUrl}/${id}`, banner).pipe(
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`).pipe(
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  uploadImage(id: number, formData: FormData): Observable<Banner> {
    return this.http.post<Banner>(`${this.apiUrl}/${id}/image`, formData).pipe(
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }
}
