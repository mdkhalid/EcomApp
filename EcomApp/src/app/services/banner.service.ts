import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Banner, CreateBanner, UpdateBanner } from '../models/banner.model';

@Injectable({
  providedIn: 'root'
})
export class BannerService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5068/api/banners';

  getAll(): Observable<Banner[]> {
    return this.http.get<Banner[]>(this.apiUrl);
  }

  getActive(): Observable<Banner[]> {
    return this.http.get<Banner[]>(`${this.apiUrl}/active`);
  }

  getById(id: number): Observable<Banner> {
    return this.http.get<Banner>(`${this.apiUrl}/${id}`);
  }

  create(banner: CreateBanner): Observable<Banner> {
    return this.http.post<Banner>(this.apiUrl, banner);
  }

  update(id: number, banner: UpdateBanner): Observable<Banner> {
    return this.http.put<Banner>(`${this.apiUrl}/${id}`, banner);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
