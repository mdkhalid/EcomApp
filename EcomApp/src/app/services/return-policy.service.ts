import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { ReturnPolicy, UpdateReturnPolicy } from '../models/return-policy.model';
import { API_URL } from '../utils/api-config';

@Injectable({ providedIn: 'root' })
export class ReturnPolicyService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = API_URL + '/returnpolicy';

  get(): Observable<ReturnPolicy> {
    return this.http.get<ReturnPolicy>(this.apiUrl).pipe(
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  update(dto: UpdateReturnPolicy): Observable<ReturnPolicy> {
    return this.http.put<ReturnPolicy>(this.apiUrl, dto).pipe(
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }
}
