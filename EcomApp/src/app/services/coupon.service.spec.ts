import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { CouponService } from './coupon.service';
import { Coupon, CreateCoupon, ValidateCouponRequest, ValidateCouponResponse } from '../models/coupon.model';
import { API_URL } from '../utils/api-config';

describe('CouponService', () => {
  let service: CouponService;
  let httpMock: HttpTestingController;
  const mockApiUrl = 'http://test-api.com';

  const mockCoupon: Coupon = {
    id: 1,
    code: 'WELCOME10',
    description: 'Welcome discount',
    discountType: 'Percentage',
    discountValue: 10,
    minOrderAmount: 50,
    maxDiscountAmount: 100,
    usageLimit: 100,
    usedCount: 10,
    validFrom: '2024-01-01T00:00:00Z',
    validTo: '2024-12-31T23:59:59Z',
    isActive: true,
    createdAt: '2024-01-01T00:00:00Z'
  };

  const mockCoupons: Coupon[] = [mockCoupon, { ...mockCoupon, id: 2, code: 'SAVE20', discountValue: 20 }];

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        CouponService,
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_URL, useValue: mockApiUrl }
      ]
    });

    service = TestBed.inject(CouponService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getAll', () => {
    it('should fetch all coupons', () => {
      service.getAll().subscribe(coupons => {
        expect(coupons).toEqual(mockCoupons);
        expect(coupons.length).toBe(2);
      });

      const req = httpMock.expectOne(`${mockApiUrl}/coupons`);
      expect(req.request.method).toBe('GET');
      req.flush(mockCoupons);
    });

    it('should handle error', () => {
      service.getAll().subscribe({
        next: () => fail('should have failed'),
        error: (error) => expect(error).toBeTruthy()
      });

      const req = httpMock.expectOne(`${mockApiUrl}/coupons`);
      req.flush('Server error', { status: 500, statusText: 'Internal Server Error' });
    });
  });

  describe('getById', () => {
    it('should fetch coupon by id', () => {
      service.getById(1).subscribe(coupon => {
        expect(coupon).toEqual(mockCoupon);
      });

      const req = httpMock.expectOne(`${mockApiUrl}/coupons/1`);
      expect(req.request.method).toBe('GET');
      req.flush(mockCoupon);
    });
  });

  describe('create', () => {
    it('should create a new coupon', () => {
      const createCoupon: CreateCoupon = {
        code: 'NEW10',
        description: 'New coupon',
        discountType: 'Percentage',
        discountValue: 10,
        minOrderAmount: 50,
        maxDiscountAmount: 100,
        usageLimit: 50,
        validFrom: '2024-01-01T00:00:00Z',
        validTo: '2024-12-31T23:59:59Z',
        isActive: true
      };

      service.create(createCoupon).subscribe(coupon => {
        expect(coupon).toEqual({ ...mockCoupon, code: 'NEW10' });
      });

      const req = httpMock.expectOne(`${mockApiUrl}/coupons`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(createCoupon);
      req.flush({ ...mockCoupon, code: 'NEW10' });
    });
  });

  describe('update', () => {
    it('should update existing coupon', () => {
      const updateData: CreateCoupon = {
        ...mockCoupon,
        discountValue: 15
      };

      service.update(1, updateData).subscribe(coupon => {
        expect(coupon.discountValue).toBe(15);
      });

      const req = httpMock.expectOne(`${mockApiUrl}/coupons/1`);
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual(updateData);
      req.flush({ ...mockCoupon, discountValue: 15 });
    });
  });

  describe('delete', () => {
    it('should delete coupon', () => {
      service.delete(1).subscribe();

      const req = httpMock.expectOne(`${mockApiUrl}/coupons/1`);
      expect(req.request.method).toBe('DELETE');
      req.flush(null);
    });
  });

  describe('validate', () => {
    it('should validate coupon successfully', () => {
      const validateRequest: ValidateCouponRequest = {
        code: 'WELCOME10',
        cartTotal: 100
      };

      const validateResponse: ValidateCouponResponse = {
        isValid: true,
        discountAmount: 10,
        coupon: mockCoupon
      };

      service.validate(validateRequest).subscribe(response => {
        expect(response).toEqual(validateResponse);
      });

      const req = httpMock.expectOne(`${mockApiUrl}/coupons/validate`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(validateRequest);
      req.flush(validateResponse);
    });

    it('should return invalid response for expired coupon', () => {
      const validateRequest: ValidateCouponRequest = {
        code: 'EXPIRED',
        cartTotal: 100
      };

      const validateResponse: ValidateCouponResponse = {
        isValid: false,
        discountAmount: 0,
        errorMessage: 'Coupon has expired'
      };

      service.validate(validateRequest).subscribe(response => {
        expect(response.isValid).toBe(false);
        expect(response.errorMessage).toBe('Coupon has expired');
      });

      const req = httpMock.expectOne(`${mockApiUrl}/coupons/validate`);
      req.flush(validateResponse);
    });
  });
});