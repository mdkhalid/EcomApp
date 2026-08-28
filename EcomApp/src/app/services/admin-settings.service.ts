import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { API_URL } from '../utils/api-config';
import { AdminSetting, SettingUpdate } from '../models/admin-settings.model';

@Injectable({ providedIn: 'root' })
export class AdminSettingsService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${API_URL}/admin/settings`;

  /** Fetch all admin-editable settings.
   *  Sensitive values are returned as "********" by the backend — never re-sent as-is. */
  getAll(): Observable<AdminSetting[]> {
    return this.http.get<AdminSetting[]>(this.apiUrl).pipe(
      catchError(err => {
        console.error('[AdminSettingsService] getAll failed', err);
        return throwError(() => err);
      })
    );
  }

  /** Persist one or more settings.
   *  The backend validates all keys before applying (no partial update on error).
   *  Sensitive fields whose value is still "********" are silently skipped server-side. */
  update(updates: SettingUpdate[]): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(this.apiUrl, updates).pipe(
      catchError(err => {
        console.error('[AdminSettingsService] update failed', err);
        return throwError(() => err);
      })
    );
  }
}
