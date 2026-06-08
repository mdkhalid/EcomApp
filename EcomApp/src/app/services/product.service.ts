import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, debounceTime, distinctUntilChanged, switchMap, of } from 'rxjs';
import { API_URL } from '../utils/api-config';

import { Product, CreateProduct, UpdateProduct, SearchFilter, SearchResult, SearchSuggestion, FilterMetadata, PriceRange } from '../models/product.model';

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

    return this.http.get<SearchResult<Product>>(this.apiUrl, { params });
  }

  getSuggestions(query: string): Observable<SearchSuggestion> {
    if (!query || query.length < 2) {
      return of({ suggestions: [], recentSearches: [], popularCategories: [] });
    }
    return this.http.get<SearchSuggestion>(`${this.apiUrl}/suggestions`, {
      params: new HttpParams().set('query', query)
    });
  }

  getFilters(filter: SearchFilter): Observable<FilterMetadata> {
    let params = new HttpParams();

    if (filter.search) params = params.set('search', filter.search);
    if (filter.category) params = params.set('category', filter.category);
    if (filter.brand) params = params.set('brand', filter.brand);
    if (filter.minPrice != null) params = params.set('minPrice', filter.minPrice.toString());
    if (filter.maxPrice != null) params = params.set('maxPrice', filter.maxPrice.toString());
    if (filter.inStock != null) params = params.set('inStock', filter.inStock.toString());

    return this.http.get<FilterMetadata>(`${this.apiUrl}/filters`, { params });
  }

  getBrands(): Observable<string[]> {
    return this.http.get<string[]>(`${this.apiUrl}/brands`);
  }

  getPriceRange(category?: string): Observable<PriceRange> {
    let params = new HttpParams();
    if (category) params = params.set('category', category);
    return this.http.get<PriceRange>(`${this.apiUrl}/price-range`, { params });
  }

  getById(id: number): Observable<Product> {
    return this.http.get<Product>(`${this.apiUrl}/${id}`);
  }

  create(product: CreateProduct): Observable<Product> {
    return this.http.post<Product>(this.apiUrl, product);
  }

  update(id: number, product: UpdateProduct): Observable<Product> {
    return this.http.put<Product>(`${this.apiUrl}/${id}`, product);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  uploadImage(id: number, file: File): Observable<Product> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<Product>(`${this.apiUrl}/${id}/image`, formData);
  }
}
