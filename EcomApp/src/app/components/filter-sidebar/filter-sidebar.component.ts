import { Component, Input, Output, EventEmitter, signal, computed, OnInit, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { FilterMetadata, SearchFilter } from '../../models/product.model';

export interface FilterState {
  categories: string[];
  brands: string[];
  minPrice: number | null;
  maxPrice: number | null;
  minRating: number | null;
  minDiscount: number | null;
  inStock: boolean;
  sortBy: string;
}

@Component({
  selector: 'app-filter-sidebar',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './filter-sidebar.component.html',
  styleUrl: './filter-sidebar.component.scss'
})
export class FilterSidebarComponent implements OnInit, OnChanges {
  @Input() filters: FilterMetadata | null = null;
  @Input() currentFilter: SearchFilter | null = null;
  @Input() totalCount = 0;
  @Output() filterChanged = new EventEmitter<FilterState>();
  @Output() clearAll = new EventEmitter<void>();

  sortOptions = [
    { value: 'popularity', label: 'Popularity' },
    { value: 'price_asc', label: 'Price -- Low to High' },
    { value: 'price_desc', label: 'Price -- High to Low' },
    { value: 'newest', label: 'Newest First' },
    { value: 'rating', label: 'Customer Rating' },
    { value: 'discount', label: 'Discount' }
  ];

  priceRanges = [
    { min: 0, max: 500, label: 'Under ₹500' },
    { min: 500, max: 1000, label: '₹500 - ₹1,000' },
    { min: 1000, max: 2000, label: '₹1,000 - ₹2,000' },
    { min: 2000, max: 5000, label: '₹2,000 - ₹5,000' },
    { min: 5000, max: 10000, label: '₹5,000 - ₹10,000' },
    { min: 10000, max: null as number | null, label: 'Above ₹10,000' }
  ];

  filterState = signal<FilterState>({
    categories: [],
    brands: [],
    minPrice: null,
    maxPrice: null,
    minRating: null,
    minDiscount: null,
    inStock: false,
    sortBy: 'popularity'
  });

  expandedSections = signal<Record<string, boolean>>({
    category: true,
    brand: true,
    price: true,
    rating: true,
    discount: true,
    availability: true
  });

  priceRange = signal<{ min: number; max: number }>({ min: 0, max: 0 });
  tempMinPrice: number | null = null;
  tempMaxPrice: number | null = null;

  ngOnInit(): void {
    this.initializeFilterState();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['filters'] || changes['currentFilter']) {
      this.initializeFilterState();
    }
  }

  private initializeFilterState(): void {
    if (this.filters) {
      this.priceRange.set({ min: this.filters.minPrice, max: this.filters.maxPrice });
      this.tempMinPrice = this.currentFilter?.minPrice ?? null;
      this.tempMaxPrice = this.currentFilter?.maxPrice ?? null;
    }

    if (this.currentFilter) {
      this.filterState.set({
        categories: this.currentFilter.category?.split(',').filter(Boolean) ?? [],
        brands: this.currentFilter.brand?.split(',').filter(Boolean) ?? [],
        minPrice: this.currentFilter.minPrice ?? null,
        maxPrice: this.currentFilter.maxPrice ?? null,
        minRating: this.currentFilter.minRating ?? null,
        minDiscount: this.currentFilter.minDiscount ?? null,
        inStock: this.currentFilter.inStock ?? false,
        sortBy: this.currentFilter.sortBy ?? 'popularity'
      });
    }
  }

  toggleSection(section: string): void {
    this.expandedSections.update(s => ({
      ...s,
      [section]: !s[section]
    }));
  }

  toggleCategory(category: string): void {
    this.filterState.update(state => {
      const categories = state.categories.includes(category)
        ? state.categories.filter(c => c !== category)
        : [...state.categories, category];
      return { ...state, categories };
    });
    this.emitFilterChange();
  }

  toggleBrand(brand: string): void {
    this.filterState.update(state => {
      const brands = state.brands.includes(brand)
        ? state.brands.filter(b => b !== brand)
        : [...state.brands, brand];
      return { ...state, brands };
    });
    this.emitFilterChange();
  }

  setMinPrice(value: number | null): void {
    this.tempMinPrice = value;
  }

  setMaxPrice(value: number | null): void {
    this.tempMaxPrice = value;
  }

  applyPriceFilter(): void {
    this.filterState.update(state => ({
      ...state,
      minPrice: this.tempMinPrice,
      maxPrice: this.tempMaxPrice
    }));
    this.emitFilterChange();
  }

  setRating(rating: number): void {
    this.filterState.update(state => ({
      ...state,
      minRating: state.minRating === rating ? null : rating
    }));
    this.emitFilterChange();
  }

  setDiscount(discount: number): void {
    this.filterState.update(state => ({
      ...state,
      minDiscount: state.minDiscount === discount ? null : discount
    }));
    this.emitFilterChange();
  }

  toggleInStock(): void {
    this.filterState.update(state => ({
      ...state,
      inStock: !state.inStock
    }));
    this.emitFilterChange();
  }

  onSortChange(sortBy: string): void {
    this.filterState.update(state => ({ ...state, sortBy }));
    this.emitFilterChange();
  }

  clearAllFilters(): void {
    this.filterState.set({
      categories: [],
      brands: [],
      minPrice: null,
      maxPrice: null,
      minRating: null,
      minDiscount: null,
      inStock: false,
      sortBy: 'popularity'
    });
    this.tempMinPrice = null;
    this.tempMaxPrice = null;
    this.clearAll.emit();
  }

  getActiveFilterCount(): number {
    const state = this.filterState();
    let count = 0;
    if (state.categories.length > 0) count++;
    if (state.brands.length > 0) count++;
    if (state.minPrice != null || state.maxPrice != null) count++;
    if (state.minRating != null) count++;
    if (state.minDiscount != null) count++;
    if (state.inStock) count++;
    return count;
  }

  private emitFilterChange(): void {
    this.filterChanged.emit(this.filterState());
  }

  formatPrice(price: number): string {
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: 'INR',
      maximumFractionDigits: 0
    }).format(price);
  }

  getBrandCount(brand: string): number {
    if (!this.filters) return 0;
    // This would come from the API in a real implementation
    return 0;
  }
}
