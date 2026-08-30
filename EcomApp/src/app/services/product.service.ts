import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { debounceTime, distinctUntilChanged, map, Observable, of, switchMap, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { API_URL } from '../utils/api-config';

import { Product, CreateProduct, UpdateProduct, SearchFilter, SearchResult, SearchSuggestion, FilterMetadata, PriceRange, ProductVariant, CreateProductVariant, ProductImage } from '../models/product.model';

@Injectable({
  providedIn: 'root'
})
export class ProductService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = API_URL + '/products';

  search(filter: SearchFilter): Observable<SearchResult<Product>> {
    let params = new HttpParams()
      .set('pageNumber', filter.pageNumber.toString())
      .set('pageSize', filter.pageSize.toString());

    if (filter.search) params = params.set('search', filter.search);
    if (filter.category) params = params.set('category', filter.category);
    if (filter.brand) params = params.set('brand', filter.brand);
    if (filter.minPrice != null) params = params.set('minPrice', filter.minPrice.toString());
    if (filter.maxPrice != null) params = params.set('maxPrice', filter.maxPrice.toString());
    if (filter.minRating != null) params = params.set('minRating', filter.minRating.toString());
    if (filter.minDiscount != null) params = params.set('minDiscount', filter.minDiscount.toString());
    if (filter.inStock != null) params = params.set('inStock', filter.inStock.toString());
    if (filter.sortBy) params = params.set('sortBy', filter.sortBy);

    return this.http.get<SearchResult<Product>>(this.apiUrl, { params }).pipe(
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  getSuggestions(query: string): Observable<SearchSuggestion> {
    if (!query || query.length < 2) {
      return of({ suggestions: [], recentSearches: [], popularCategories: [] });
    }
    return this.http.get<SearchSuggestion>(`${this.apiUrl}/suggestions`, {
      params: new HttpParams().set('query', query)
    }).pipe(
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  getFilters(filter: SearchFilter): Observable<FilterMetadata> {
    let params = new HttpParams();

    if (filter.search) params = params.set('search', filter.search);
    if (filter.category) params = params.set('category', filter.category);
    if (filter.brand) params = params.set('brand', filter.brand);
    if (filter.minPrice != null) params = params.set('minPrice', filter.minPrice.toString());
    if (filter.maxPrice != null) params = params.set('maxPrice', filter.maxPrice.toString());
    if (filter.inStock != null) params = params.set('inStock', filter.inStock.toString());

    return this.http.get<FilterMetadata>(`${this.apiUrl}/filters`, { params }).pipe(
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  getBrands(): Observable<string[]> {
    return this.http.get<string[]>(`${this.apiUrl}/brands`).pipe(
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  getPriceRange(category?: string): Observable<PriceRange> {
    let params = new HttpParams();
    if (category) params = params.set('category', category);
    return this.http.get<PriceRange>(`${this.apiUrl}/price-range`, { params }).pipe(
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  getById(id: number): Observable<Product> {
    return this.http.get<Product>(`${this.apiUrl}/${id}`).pipe(
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  create(product: CreateProduct): Observable<Product> {
    return this.http.post<Product>(this.apiUrl, product).pipe(
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  update(id: number, product: UpdateProduct): Observable<Product> {
    return this.http.put<Product>(`${this.apiUrl}/${id}`, product).pipe(
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`).pipe(
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  getAll(): Observable<Product[]> {
    return this.search({ pageNumber: 1, pageSize: 1000 }).pipe(
      map(res => res.items),
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  uploadImage(id: number, file: File | FormData): Observable<Product> {
    if (file instanceof FormData) {
      return this.http.post<Product>(`${this.apiUrl}/${id}/image`, file).pipe(
        catchError(err => { console.error(err); return throwError(() => err); })
      );
    }
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<Product>(`${this.apiUrl}/${id}/image`, formData).pipe(
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  deleteImage(imageId: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/images/${imageId}`).pipe(
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  getVariants(productId: number): Observable<ProductVariant[]> {
    return this.http.get<ProductVariant[]>(`${this.apiUrl}/${productId}/variants`).pipe(
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  addVariant(productId: number, variant: CreateProductVariant): Observable<ProductVariant> {
    return this.http.post<ProductVariant>(`${this.apiUrl}/${productId}/variants`, variant).pipe(
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  updateVariant(productId: number, variantId: number, variant: CreateProductVariant): Observable<ProductVariant> {
    return this.http.put<ProductVariant>(`${this.apiUrl}/${productId}/variants/${variantId}`, variant).pipe(
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }

  deleteVariant(productId: number, variantId: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${productId}/variants/${variantId}`).pipe(
      catchError(err => { console.error(err); return throwError(() => err); })
    );
  }
}
