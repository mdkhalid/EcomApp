import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const customerGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isAuthenticated()) {
    router.navigate(['/login'], { queryParams: { returnUrl: router.url } });
    return false;
  }

  if (authService.isAdmin()) {
    router.navigate(['/admin'], { queryParams: { denied: 'true' } });
    return false;
  }

  return true;
};
