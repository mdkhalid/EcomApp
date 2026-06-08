import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ReturnPolicy, UpdateReturnPolicy } from '../models/return-policy.model';

@Injectable({ providedIn: 'root' })
export class ReturnPolicyService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5068/api/returnpolicy';

  get() {
    return this.http.get<ReturnPolicy>(this.apiUrl);
  }

  update(dto: UpdateReturnPolicy) {
    return this.http.put<ReturnPolicy>(this.apiUrl, dto);
  }
}
