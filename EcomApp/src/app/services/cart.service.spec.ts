import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { CartService } from './cart.service';
import { Cart, AddCartItem } from '../models/cart.model';
import { API_URL } from '../utils/api-config';

describe('CartService', () => {
  let service: CartService;
  let httpMock: HttpTestingController;
  const mockApiUrl = 'http://test-api.com';

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        CartService,
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_URL, useValue: mockApiUrl }
      ]
    });

    service = TestBed.inject(CartService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should initialize cartItemCount to 0', () => {
    expect(service.cartItemCount()).toBe(0);
  });

  describe('getCart', () => {
    it('should fetch cart and update count', () => {
      const mockCart: Cart = {
        id: 1,
        items: [
          { id: 1, productId: 1, productName: 'Product 1', quantity: 2, unitPrice: 10, totalPrice: 20, productImage: '' },
          { id: 2, productId: 2, productName: 'Product 2', quantity: 1, unitPrice: 15, totalPrice: 15, productImage: '' }
        ],
        subtotal: 35,
        discount: 0,
        total: 35,
        couponCode: null
      };

      service.getCart().subscribe(cart => {
        expect(cart).toEqual(mockCart);
        expect(service.cartItemCount()).toBe(3);
      });

      const req = httpMock.expectOne(`${mockApiUrl}/carts`);
      expect(req.request.method).toBe('GET');
      req.flush(mockCart);
    });

    it('should handle error', () => {
      service.getCart().subscribe({
        next: () => fail('should have failed'),
        error: (error) => expect(error).toBeTruthy()
      });

      const req = httpMock.expectOne(`${mockApiUrl}/carts`);
      req.flush('Server error', { status: 500, statusText: 'Internal Server Error' });
    });
  });

  describe('addItem', () => {
    it('should add item to cart and update count', () => {
      const item: AddCartItem = { productId: 1, variantId: null, quantity: 2 };
      const mockCart: Cart = {
        id: 1,
        items: [
          { id: 1, productId: 1, productName: 'Product 1', quantity: 2, unitPrice: 10, totalPrice: 20, productImage: '' }
        ],
        subtotal: 20,
        discount: 0,
        total: 20,
        couponCode: null
      };

      service.addItem(item).subscribe(cart => {
        expect(cart).toEqual(mockCart);
        expect(service.cartItemCount()).toBe(2);
      });

      const req = httpMock.expectOne(`${mockApiUrl}/carts/items`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(item);
      req.flush(mockCart);
    });
  });

  describe('updateItem', () => {
    it('should update cart item and update count', () => {
      const updateItem = { quantity: 3 };
      const mockCart: Cart = {
        id: 1,
        items: [
          { id: 1, productId: 1, productName: 'Product 1', quantity: 3, unitPrice: 10, totalPrice: 30, productImage: '' }
        ],
        subtotal: 30,
        discount: 0,
        total: 30,
        couponCode: null
      };

      service.updateItem(1, updateItem).subscribe(cart => {
        expect(cart).toEqual(mockCart);
        expect(service.cartItemCount()).toBe(3);
      });

      const req = httpMock.expectOne(`${mockApiUrl}/carts/items/1`);
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual(updateItem);
      req.flush(mockCart);
    });
  });

  describe('removeItem', () => {
    it('should remove item and update count', () => {
      const mockCart: Cart = {
        id: 1,
        items: [],
        subtotal: 0,
        discount: 0,
        total: 0,
        couponCode: null
      };

      service.removeItem(1).subscribe(cart => {
        expect(cart).toEqual(mockCart);
        expect(service.cartItemCount()).toBe(0);
      });

      const req = httpMock.expectOne(`${mockApiUrl}/carts/items/1`);
      expect(req.request.method).toBe('DELETE');
      req.flush(mockCart);
    });
  });

  describe('clearCart', () => {
    it('should clear cart and reset count', () => {
      const mockCart: Cart = {
        id: 1,
        items: [],
        subtotal: 0,
        discount: 0,
        total: 0,
        couponCode: null
      };

      service.clearCart().subscribe(cart => {
        expect(cart).toEqual(mockCart);
        expect(service.cartItemCount()).toBe(0);
      });

      const req = httpMock.expectOne(`${mockApiUrl}/carts`);
      expect(req.request.method).toBe('DELETE');
      req.flush(mockCart);
    });
  });

  describe('mergeCart', () => {
    it('should merge cart successfully', () => {
      const mockResponse = { message: 'Cart merged successfully' };

      service.mergeCart().subscribe(response => {
        expect(response).toEqual(mockResponse);
      });

      const req = httpMock.expectOne(`${mockApiUrl}/carts/merge`);
      expect(req.request.method).toBe('POST');
      req.flush(mockResponse);
    });
  });

  describe('resetCount', () => {
    it('should reset cart item count to 0', () => {
      service.cartItemCount.set(5);
      service.resetCount();
      expect(service.cartItemCount()).toBe(0);
    });
  });
});