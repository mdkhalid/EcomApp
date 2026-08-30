import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { authGuard, guestGuard, adminGuard, customerGuard } from '../guards';

describe('Auth Guards', () => {
  let router: jasmine.SpyObj<Router>;
  let authService: jasmine.SpyObj<{
    isAuthenticated: () => boolean;
    isAdmin: () => boolean;
    isSubAdmin: () => boolean;
    currentUser: () => { role: string } | null;
  }>;

  const mockUser = { role: 'Customer' };
  const mockAdminUser = { role: 'Admin' };

  beforeEach(() => {
    router = jasmine.createSpyObj('Router', ['navigate']);
    authService = jasmine.createSpyObj('AuthService', ['isAuthenticated', 'isAdmin', 'isSubAdmin', 'currentUser']);

    TestBed.configureTestingModule({
      providers: [
        { provide: Router, useValue: router },
        { provide: 'AuthService', useValue: authService }
      ]
    });
  });

  afterEach(() => {
    jasmine.clock().uninstall();
  });

  function runGuard<T>(guard: (...args: unknown[]) => T): T {
    return TestBed.runInInjectionContext(() => guard(null as any, null as any));
  }

  describe('authGuard', () => {
    it('should return true when user is authenticated', () => {
      authService.isAuthenticated.and.returnValue(true);

      const result = runGuard(authGuard);

      expect(result).toBe(true);
      expect(router.navigate).not.toHaveBeenCalled();
    });

    it('should navigate to login and return false when not authenticated', () => {
      authService.isAuthenticated.and.returnValue(false);

      const result = runGuard(authGuard);

      expect(result).toBe(false);
      expect(router.navigate).toHaveBeenCalledWith(['/login'], { queryParams: { returnUrl: '/' } });
    });
  });

  describe('guestGuard', () => {
    it('should return true when user is not authenticated', () => {
      authService.isAuthenticated.and.returnValue(false);

      const result = runGuard(guestGuard);

      expect(result).toBe(true);
      expect(router.navigate).not.toHaveBeenCalled();
    });

    it('should navigate admin to /admin when authenticated as admin', () => {
      authService.isAuthenticated.and.returnValue(true);
      authService.isAdmin.and.returnValue(true);

      const result = runGuard(guestGuard);

      expect(result).toBe(false);
      expect(router.navigate).toHaveBeenCalledWith(['/admin']);
    });

    it('should navigate customer to /products when authenticated as customer', () => {
      authService.isAuthenticated.and.returnValue(true);
      authService.isAdmin.and.returnValue(false);

      const result = runGuard(guestGuard);

      expect(result).toBe(false);
      expect(router.navigate).toHaveBeenCalledWith(['/products']);
    });
  });

  describe('adminGuard', () => {
    it('should return true when user is authenticated and is admin', () => {
      authService.isAuthenticated.and.returnValue(true);
      authService.isAdmin.and.returnValue(true);

      const result = runGuard(adminGuard);

      expect(result).toBe(true);
      expect(router.navigate).not.toHaveBeenCalled();
    });

    it('should navigate to login when not authenticated', () => {
      authService.isAuthenticated.and.returnValue(false);

      const result = runGuard(adminGuard);

      expect(result).toBe(false);
      expect(router.navigate).toHaveBeenCalledWith(['/login'], { queryParams: { returnUrl: '/' } });
    });

    it('should navigate to products when authenticated but not admin', () => {
      authService.isAuthenticated.and.returnValue(true);
      authService.isAdmin.and.returnValue(false);

      const result = runGuard(adminGuard);

      expect(result).toBe(false);
      expect(router.navigate).toHaveBeenCalledWith(['/products']);
    });
  });

  describe('customerGuard', () => {
    it('should return true when user is authenticated and is customer', () => {
      authService.isAuthenticated.and.returnValue(true);
      authService.isAdmin.and.returnValue(false);

      const result = runGuard(customerGuard);

      expect(result).toBe(true);
      expect(router.navigate).not.toHaveBeenCalled();
    });

    it('should navigate to login when not authenticated', () => {
      authService.isAuthenticated.and.returnValue(false);

      const result = runGuard(customerGuard);

      expect(result).toBe(false);
      expect(router.navigate).toHaveBeenCalledWith(['/login'], { queryParams: { returnUrl: '/' } });
    });

    it('should navigate to admin with denied param when admin tries to access customer route', () => {
      authService.isAuthenticated.and.returnValue(true);
      authService.isAdmin.and.returnValue(true);

      const result = runGuard(customerGuard);

      expect(result).toBe(false);
      expect(router.navigate).toHaveBeenCalledWith(['/admin'], { queryParams: { denied: 'true' } });
    });
  });
});