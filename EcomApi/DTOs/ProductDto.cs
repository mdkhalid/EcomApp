using System.ComponentModel.DataAnnotations;

namespace EcomApi.DTOs;

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? OriginalPrice { get; set; }
    public int Stock { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; }
    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public int DiscountPercent { get; set; }
}

public class CreateProductDto
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Range(0.01, 999999.99)]
    public decimal Price { get; set; }

    [Range(0, 999999.99)]
    public decimal? OriginalPrice { get; set; }

    [Range(0, int.MaxValue)]
    public int Stock { get; set; }

    [Required]
    [StringLength(100)]
    public string Category { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Brand { get; set; }
}

public class UpdateProductDto
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Range(0.01, 999999.99)]
    public decimal Price { get; set; }

    [Range(0, 999999.99)]
    public decimal? OriginalPrice { get; set; }

    [Range(0, int.MaxValue)]
    public int Stock { get; set; }

    [Required]
    [StringLength(100)]
    public string Category { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Brand { get; set; }

    [StringLength(500)]
    public string? ImageUrl { get; set; }
}