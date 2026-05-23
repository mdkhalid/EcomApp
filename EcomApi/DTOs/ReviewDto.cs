using System.ComponentModel.DataAnnotations;

namespace EcomApi.DTOs;

public class ReviewDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateReviewDto
{
    [Required]
    [Range(1, 5)]
    public int Rating { get; set; }

    [StringLength(2000)]
    public string Comment { get; set; } = string.Empty;
}

public class ProductRatingDto
{
    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }
}
