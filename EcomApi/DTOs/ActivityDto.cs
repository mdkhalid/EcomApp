using System.ComponentModel.DataAnnotations;
using EcomApi.Models;

namespace EcomApi.DTOs;

public class LogActivityDto
{
    [Required]
    public string Type { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Data { get; set; } = string.Empty;
}

public class UserActivityDto
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Data { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class RecentlyViewedProductDto
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
    public decimal? OriginalPrice { get; set; }
    public int DiscountPercent { get; set; }
    public string Category { get; set; } = string.Empty;
    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }
}
