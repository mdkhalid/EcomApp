using EcomApi.Data;
using EcomApi.DTOs;
using EcomApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EcomApi.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly ApplicationDbContext _context;

    public ProductRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SearchResultDto<Product>> SearchProductsAsync(SearchFilterDto filter)
    {
        var query = _context.Products.AsNoTracking().Where(p => p.IsActive).AsQueryable();

        // Full-text search across name, description, brand, category
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var searchTerm = filter.Search.Trim().ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(searchTerm) ||
                p.Description.ToLower().Contains(searchTerm) ||
                (p.Brand != null && p.Brand.ToLower().Contains(searchTerm)) ||
                p.Category.ToLower().Contains(searchTerm));
        }

        // Category filter
        if (!string.IsNullOrWhiteSpace(filter.Category))
        {
            var categoryList = filter.Category.Split(',', StringSplitOptions.RemoveEmptyEntries);
            query = query.Where(p => categoryList.Contains(p.Category));
        }

        // Brand filter
        if (!string.IsNullOrWhiteSpace(filter.Brand))
        {
            var brandList = filter.Brand.Split(',', StringSplitOptions.RemoveEmptyEntries);
            query = query.Where(p => p.Brand != null && brandList.Contains(p.Brand));
        }

        // Price range filter
        if (filter.MinPrice.HasValue)
            query = query.Where(p => p.Price >= filter.MinPrice.Value);

        if (filter.MaxPrice.HasValue)
            query = query.Where(p => p.Price <= filter.MaxPrice.Value);

        // Rating filter
        if (filter.MinRating.HasValue)
        {
            query = query.Where(p =>
                _context.Reviews
                    .Where(r => r.ProductId == p.Id)
                    .Average(r => (double?)r.Rating) >= filter.MinRating.Value);
        }

        // Discount filter
        if (filter.MinDiscount.HasValue)
            query = query.Where(p => p.OriginalPrice.HasValue && p.OriginalPrice > 0 &&
                ((1 - p.Price / p.OriginalPrice.Value) * 100) >= filter.MinDiscount.Value);

        if (filter.MaxDiscount.HasValue)
            query = query.Where(p => p.OriginalPrice.HasValue && p.OriginalPrice > 0 &&
                ((1 - p.Price / p.OriginalPrice.Value) * 100) <= filter.MaxDiscount.Value);

        // In-stock filter
        if (filter.InStock.HasValue && filter.InStock.Value)
            query = query.Where(p => p.Stock > 0);

        var totalCount = await query.CountAsync();

        // Apply sorting
        query = ApplySorting(query, filter.SortBy);

        // Apply pagination
        var items = await query
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new SearchResultDto<Product>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };
    }

    public async Task<List<string>> GetSearchSuggestionsAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<string>();

        var searchTerm = query.Trim().ToLower();

        var suggestions = await _context.Products.AsNoTracking()
            .Where(p => p.IsActive &&
                (p.Name.ToLower().Contains(searchTerm) ||
                 p.Brand!.ToLower().Contains(searchTerm) ||
                 p.Category.ToLower().Contains(searchTerm)))
            .Select(p => p.Name)
            .Distinct()
            .Take(10)
            .ToListAsync();

        // Also add matching brands
        var brandSuggestions = await _context.Products.AsNoTracking()
            .Where(p => p.IsActive && p.Brand != null &&
                p.Brand.ToLower().Contains(searchTerm))
            .Select(p => p.Brand!)
            .Distinct()
            .Take(5)
            .ToListAsync();

        // Also add matching categories
        var categorySuggestions = await _context.Products.AsNoTracking()
            .Where(p => p.IsActive &&
                p.Category.ToLower().Contains(searchTerm))
            .Select(p => p.Category)
            .Distinct()
            .Take(5)
            .ToListAsync();

        return suggestions
            .Union(brandSuggestions)
            .Union(categorySuggestions)
            .Distinct()
            .Take(10)
            .ToList();
    }

    public async Task<FilterMetadataDto> GetFilterMetadataAsync(SearchFilterDto filter)
    {
        var query = _context.Products.AsNoTracking().Where(p => p.IsActive).AsQueryable();

        // Apply same filters except rating and discount
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var searchTerm = filter.Search.Trim().ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(searchTerm) ||
                p.Description.ToLower().Contains(searchTerm) ||
                (p.Brand != null && p.Brand.ToLower().Contains(searchTerm)) ||
                p.Category.ToLower().Contains(searchTerm));
        }

        if (!string.IsNullOrWhiteSpace(filter.Category))
        {
            var categoryList = filter.Category.Split(',', StringSplitOptions.RemoveEmptyEntries);
            query = query.Where(p => categoryList.Contains(p.Category));
        }

        if (!string.IsNullOrWhiteSpace(filter.Brand))
        {
            var brandList = filter.Brand.Split(',', StringSplitOptions.RemoveEmptyEntries);
            query = query.Where(p => p.Brand != null && brandList.Contains(p.Brand));
        }

        if (filter.MinPrice.HasValue)
            query = query.Where(p => p.Price >= filter.MinPrice.Value);

        if (filter.MaxPrice.HasValue)
            query = query.Where(p => p.Price <= filter.MaxPrice.Value);

        if (filter.InStock.HasValue && filter.InStock.Value)
            query = query.Where(p => p.Stock > 0);

        // Get categories with counts
        var categories = await query
            .GroupBy(p => p.Category)
            .Select(g => new { Category = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Select(x => x.Category)
            .ToListAsync();

        // Get brands with counts
        var brands = await query
            .Where(p => p.Brand != null)
            .GroupBy(p => p.Brand)
            .Select(g => new { Brand = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Select(x => x.Brand!)
            .ToListAsync();

        // Get price range
        var priceRange = await query
            .GroupBy(p => 1)
            .Select(g => new
            {
                Min = g.Min(p => p.Price),
                Max = g.Max(p => p.Price)
            })
            .FirstOrDefaultAsync();

        // Calculate price step (10% of range or minimum 10)
        var priceStep = priceRange != null ? Math.Max(10, (priceRange.Max - priceRange.Min) / 10) : 10;

        // Get rating buckets
        var ratingBuckets = new List<RatingBucketDto>();
        var allProducts = await query.ToListAsync();
        foreach (var bucket in new[] {
            new { Min = 4.0, Max = 5.0, Label = "4★ & above" },
            new { Min = 3.0, Max = 4.0, Label = "3★ & above" },
            new { Min = 2.0, Max = 3.0, Label = "2★ & above" },
            new { Min = 1.0, Max = 2.0, Label = "1★ & above" }
        })
        {
            var count = await query
                .Where(p =>
                    _context.Reviews
                        .Where(r => r.ProductId == p.Id)
                        .Average(r => (double?)r.Rating) >= bucket.Min)
                .CountAsync();

            ratingBuckets.Add(new RatingBucketDto
            {
                MinRating = bucket.Min,
                MaxRating = bucket.Max,
                Count = count,
                Label = bucket.Label
            });
        }

        // Get discount buckets
        var discountBuckets = new List<DiscountBucketDto>();
        foreach (var bucket in new[] {
            new { Min = 50, Max = 100, Label = "50% or more" },
            new { Min = 30, Max = 50, Label = "30% or more" },
            new { Min = 20, Max = 30, Label = "20% or more" },
            new { Min = 10, Max = 20, Label = "10% or more" }
        })
        {
            var count = await query
                .Where(p => p.OriginalPrice.HasValue && p.OriginalPrice > 0 &&
                    ((1 - p.Price / p.OriginalPrice.Value) * 100) >= bucket.Min)
                .CountAsync();

            discountBuckets.Add(new DiscountBucketDto
            {
                MinDiscount = bucket.Min,
                MaxDiscount = bucket.Max,
                Count = count,
                Label = bucket.Label
            });
        }

        return new FilterMetadataDto
        {
            Categories = categories,
            Brands = brands,
            MinPrice = priceRange?.Min ?? 0,
            MaxPrice = priceRange?.Max ?? 0,
            PriceStep = priceStep,
            RatingBuckets = ratingBuckets,
            DiscountBuckets = discountBuckets
        };
    }

    public async Task<List<string>> GetBrandsAsync()
    {
        return await _context.Products.AsNoTracking()
            .Where(p => p.IsActive && p.Brand != null)
            .Select(p => p.Brand!)
            .Distinct()
            .OrderBy(b => b)
            .ToListAsync();
    }

    public async Task<PriceRangeDto> GetPriceRangeAsync(string? category = null)
    {
        var query = _context.Products.AsNoTracking().Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(category))
        {
            var categoryList = category.Split(',', StringSplitOptions.RemoveEmptyEntries);
            query = query.Where(p => categoryList.Contains(p.Category));
        }

        var range = await query
            .GroupBy(p => 1)
            .Select(g => new
            {
                Min = g.Min(p => p.Price),
                Max = g.Max(p => p.Price)
            })
            .FirstOrDefaultAsync();

        var step = range != null ? Math.Max(10, (range.Max - range.Min) / 10) : 10;

        return new PriceRangeDto
        {
            Min = range?.Min ?? 0,
            Max = range?.Max ?? 0,
            Step = step
        };
    }

    private static IQueryable<Product> ApplySorting(IQueryable<Product> query, string? sortBy)
    {
        return sortBy switch
        {
            "price_asc" => query.OrderBy(p => p.Price),
            "price_desc" => query.OrderByDescending(p => p.Price),
            "newest" => query.OrderByDescending(p => p.CreatedAt),
            "rating" => query.OrderByDescending(p =>
                p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 0),
            "popularity" => query.OrderByDescending(p =>
                p.Reviews.Count),
            "discount" => query.OrderByDescending(p =>
                p.OriginalPrice.HasValue && p.OriginalPrice > 0
                    ? (1 - p.Price / p.OriginalPrice.Value) * 100
                    : 0),
            _ => query.OrderByDescending(p => p.Id)
        };
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        return await _context.Products.FindAsync(id);
    }

    public async Task<Product> AddAsync(Product product)
    {
        product.UpdatedAt = DateTime.UtcNow;
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return product;
    }

    public async Task<Product> UpdateAsync(Product product)
    {
        product.UpdatedAt = DateTime.UtcNow;
        _context.Entry(product).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return product;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return false;

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
        return true;
    }
}
