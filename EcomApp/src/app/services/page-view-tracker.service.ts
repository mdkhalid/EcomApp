import { Injectable, inject, NgZone } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';
import { API_URL } from '../utils/api-config';

@Injectable({ providedIn: 'root' })
export class PageViewTrackerService {
  private readonly router = inject(Router);
  private readonly ngZone = inject(NgZone);

  constructor() {
    this.ngZone.runOutsideAngular(() => {
      this.router.events
        .pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd))
        .subscribe((e) => {
          this.track(e.urlAfterRedirects, document.referrer);
        });
    });
  }

  private track(path: string, referrer: string): void {
    const token = localStorage.getItem('accessToken');
    if (!token) return;

    try {
      fetch(`${API_URL}/pagetracking/track`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`
        },
        body: JSON.stringify({ path, referrer: referrer || undefined }),
        keepalive: true
      }).catch(() => {});
    } catch {
      // fire-and-forget, ignore errors
    }
  }
}
