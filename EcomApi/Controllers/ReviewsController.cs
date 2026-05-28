using System.Security.Claims;
using EcomApi.DTOs;
using EcomApi.Models;
using EcomApi.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcomApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewsController : ControllerBase
{
    private readonly IReviewRepository _repository;
    private readonly IOrderRepository _orderRepository;
    private readonly IUserRepository _userRepository;

    public ReviewsController(IReviewRepository repository, IOrderRepository orderRepository, IUserRepository userRepository)
    {
        _repository = repository;
        _orderRepository = orderRepository;
        _userRepository = userRepository;
    }

    [HttpGet("product/{productId}")]
    public async Task<ActionResult> GetByProduct(
        int productId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var (items, totalCount) = await _repository.GetByProductIdAsync(productId, pageNumber, pageSize);

        var rating = await _repository.GetProductRatingAsync(productId);

        return Ok(new
        {
            items = items.Select(r => new ReviewDto
            {
                Id = r.Id,
                ProductId = r.ProductId,
                UserId = r.UserId,
                UserName = r.User.Username,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt
            }),
            totalCount,
            pageNumber,
            pageSize,
            averageRating = rating.AverageRating,
            totalReviews = rating.TotalReviews
        });
    }

    [HttpGet("product/{productId}/can-review")]
    [Authorize]
    public async Task<ActionResult> CanReview(int productId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var existing = await _repository.GetByUserAndProductAsync(userId, productId);
        if (existing != null)
            return Ok(new { canReview = false, reason = "You have already reviewed this product." });

        var hasOrdered = await _orderRepository.HasUserPurchasedProductAsync(userId, productId);
        if (!hasOrdered)
            return Ok(new { canReview = false, reason = "You can only review products you have purchased." });

        return Ok(new { canReview = true, reason = "" });
    }

    [HttpGet("product/{productId}/rating")]
    public async Task<ActionResult<ProductRatingDto>> GetProductRating(int productId)
    {
        var (averageRating, totalReviews) = await _repository.GetProductRatingAsync(productId);
        return Ok(new ProductRatingDto { AverageRating = averageRating, TotalReviews = totalReviews });
    }

    [HttpPost("product/{productId}")]
    [Authorize]
    public async Task<ActionResult<ReviewDto>> Create(int productId, [FromBody] CreateReviewDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var existing = await _repository.GetByUserAndProductAsync(userId, productId);
        if (existing != null)
            return BadRequest(new { error = "You have already reviewed this product." });

        var hasOrdered = await _orderRepository.HasUserPurchasedProductAsync(userId, productId);
        if (!hasOrdered)
            return BadRequest(new { error = "You can only review products you have purchased." });

        var review = new Review
        {
            ProductId = productId,
            UserId = userId,
            Rating = dto.Rating,
            Comment = dto.Comment
        };

        review = await _repository.CreateAsync(review);

        var userName = (await _userRepository.GetByIdAsync(userId))?.Username ?? "User";

        return Ok(new ReviewDto
        {
            Id = review.Id,
            ProductId = review.ProductId,
            UserId = review.UserId,
            UserName = userName,
            Rating = review.Rating,
            Comment = review.Comment,
            CreatedAt = review.CreatedAt
        });
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<ActionResult> Delete(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var role = User.FindFirstValue(ClaimTypes.Role);

        // Try to find the review directly by ID
        var review = await _repository.GetByIdAsync(id);

        // Only the review author or admin can delete
        if (review == null || (review.UserId != userId && role != "Admin"))
            return NotFound();

        var deleted = await _repository.DeleteAsync(id);
        if (!deleted)
            return NotFound();

        return NoContent();
    }
}
