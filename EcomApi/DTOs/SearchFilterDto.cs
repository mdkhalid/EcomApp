using System.ComponentModel.DataAnnotations;

namespace EcomApi.DTOs;

public class SearchFilterDto
{
    [StringLength(200)]
    public string? Search { get; set; }

    [StringLength(100)]
    public string? Category { get; set; }

    [StringLength(100)]
    public string? Brand { get; set; }

    [Range(0, 999999.99)]
    public decimal? MinPrice { get; set; }

    [Range(0, 999999.99)]
    public decimal? MaxPrice { get; set; }

    [Range(0, 5)]
    public double? MinRating { get; set; }

    [Range(0, 100)]
    public int? MinDiscount { get; set; }

    [Range(0, 100)]
    public int? MaxDiscount { get; set; }

    public bool? InStock { get; set; }

    [StringLength(50)]
    public string? SortBy { get; set; }

    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 1000)]
    public int PageSize { get; set; } = 12;
}

public class SearchResultDto<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public FilterMetadataDto Filters { get; set; } = new();
}

public class FilterMetadataDto
{
    public List<string> Categories { get; set; } = new();
    public List<string> Brands { get; set; } = new();
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
    public decimal PriceStep { get; set; }
    public List<RatingBucketDto> RatingBuckets { get; set; } = new();
    public List<DiscountBucketDto> DiscountBuckets { get; set; } = new();
}

public class RatingBucketDto
{
    public double MinRating { get; set; }
    public double MaxRating { get; set; }
    public int Count { get; set; }
    public string Label { get; set; } = string.Empty;
}

public class DiscountBucketDto
{
    public int MinDiscount { get; set; }
    public int MaxDiscount { get; set; }
    public int Count { get; set; }
    public string Label { get; set; } = string.Empty;
}

public class SearchSuggestionDto
{
    public List<string> Suggestions { get; set; } = new();
    public List<string> RecentSearches { get; set; } = new();
    public List<string> PopularCategories { get; set; } = new();
}

public class PriceRangeDto
{
    public decimal Min { get; set; }
    public decimal Max { get; set; }
    public decimal Step { get; set; }
}

public class ActiveFiltersDto
{
    public string? Search { get; set; }
    public string? Category { get; set; }
    public string? Brand { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public double? MinRating { get; set; }
    public int? MinDiscount { get; set; }
    public bool? InStock { get; set; }
    public string? SortBy { get; set; }
}
