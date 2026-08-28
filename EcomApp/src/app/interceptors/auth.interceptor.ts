import { HttpInterceptorFn, HttpRequest, HttpHandlerFn, HttpEvent, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { Observable, throwError, BehaviorSubject } from 'rxjs';
import { catchError, filter, switchMap, take } from 'rxjs/operators';

// ---------------------------------------------------------------------------
// Shared refresh state — lives for the lifetime of the app (module scope).
// BehaviorSubject(null) = not refreshing; BehaviorSubject(token) = done.
// ---------------------------------------------------------------------------
let isRefreshing = false;
const refreshDone$ = new BehaviorSubject<string | null>(null);

/**
 * Auth interceptor — attaches Bearer token to every outgoing request and
 * handles transparent JWT refresh on 401:
 *
 *  1. Attach the stored access token as Authorization: Bearer.
 *  2. On 401 response:
 *     a. Skip the refresh/logout endpoints themselves (avoid loops).
 *     b. If a refresh is already in flight, queue this request and replay
 *        it once the new token arrives.
 *     c. Otherwise call AuthService.refresh(), then replay the original.
 *  3. If the refresh call itself fails (expired/revoked token), logout
 *     and navigate to /login.
 */
export const authInterceptor: HttpInterceptorFn = (
  req: HttpRequest<unknown>,
  next: HttpHandlerFn
): Observable<HttpEvent<unknown>> => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return next(addToken(req)).pipe(
    catchError((error: HttpErrorResponse) => {
      // Only intercept 401 Unauthorized responses
      if (error.status !== 401) {
        return throwError(() => error);
      }

      // Do NOT attempt refresh for the auth endpoints themselves —
      // that would create an infinite loop.
      if (isAuthEndpoint(req.url)) {
        authService.logout();
        router.navigate(['/login']);
        return throwError(() => error);
      }

      if (isRefreshing) {
        // A refresh is already in flight — wait for it, then replay.
        return refreshDone$.pipe(
          filter((token): token is string => token !== null),
          take(1),
          switchMap(newToken => next(addToken(req, newToken)))
        );
      }

      // Start a new refresh cycle.
      isRefreshing = true;
      refreshDone$.next(null); // signal "refreshing…"

      return authService.refresh().pipe(
        switchMap(() => {
          const newToken = localStorage.getItem('accessToken')!;
          isRefreshing = false;
          refreshDone$.next(newToken); // unblock queued requests
          return next(addToken(req, newToken));
        }),
        catchError(refreshError => {
          // Refresh failed — clean up state, logout, navigate.
          isRefreshing = false;
          refreshDone$.next(null);
          authService.logout();
          router.navigate(['/login']);
          return throwError(() => refreshError);
        })
      );
    })
  );
};

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

/** Clone the request with the current (or provided) access token attached. */
function addToken(req: HttpRequest<unknown>, token?: string): HttpRequest<unknown> {
  const accessToken = token ?? localStorage.getItem('accessToken');
  if (accessToken) {
    return req.clone({
      setHeaders: { Authorization: `Bearer ${accessToken}` },
      withCredentials: true
    });
  }
  return req.clone({ withCredentials: true });
}

/** Returns true for endpoints that must never trigger a refresh loop. */
function isAuthEndpoint(url: string): boolean {
  return (
    url.includes('/auth/refresh') ||
    url.includes('/auth/logout') ||
    url.includes('/auth/login') ||
    url.includes('/auth/register') ||
    url.includes('/auth/forgot-password') ||
    url.includes('/auth/reset-password')
  );
}
