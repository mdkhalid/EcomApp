using System.Security.Claims;
using EcomApi.Data;
using EcomApi.DTOs;
using EcomApi.Models;
using EcomApi.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcomApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RecommendationsController : ControllerBase
{
    private readonly IActivityRepository _activityRepository;
    private readonly ApplicationDbContext _context;

    public RecommendationsController(IActivityRepository activityRepository, ApplicationDbContext context)
    {
        _activityRepository = activityRepository;
        _context = context;
    }

    [HttpGet("recently-viewed")]
    [Authorize]
    public async Task<ActionResult<List<RecentlyViewedProductDto>>> GetRecentlyViewed()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var sessionId = Request.Cookies.TryGetValue("CartId", out var sid) ? sid : null;

        var productIds = await _activityRepository.GetRecentProductIdsAsync(userId, sessionId, 20);

        if (productIds.Count == 0)
            return Ok(new List<RecentlyViewedProductDto>());

        var products = await _context.Products
            .AsNoTracking()
            .Include(p => p.Reviews)
            .Where(p => productIds.Contains(p.Id) && p.IsActive)
            .ToListAsync();

        var ordered = productIds
            .Select(id => products.FirstOrDefault(p => p.Id == id))
            .Where(p => p != null)
            .Select(p => MapProduct(p!))
            .ToList();

        return Ok(ordered);
    }

    [HttpGet("for-you")]
    [Authorize]
    public async Task<ActionResult<List<RecentlyViewedProductDto>>> GetForYou()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var productIds = await _activityRepository.GetRecommendedProductIdsAsync(userId, 10);

        if (productIds.Count == 0)
        {
            var trending = await _context.Products
                .AsNoTracking()
                .Include(p => p.Reviews)
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.Reviews.Average(r => (double?)r.Rating) ?? 0)
                .ThenByDescending(p => p.Reviews.Count)
                .Take(10)
                .ToListAsync();

            return Ok(trending.Select(MapProduct).ToList());
        }

        var products = await _context.Products
            .AsNoTracking()
            .Include(p => p.Reviews)
            .Where(p => productIds.Contains(p.Id) && p.IsActive)
            .ToListAsync();

        var ordered = productIds
            .Select(id => products.FirstOrDefault(p => p.Id == id))
            .Where(p => p != null)
            .Select(p => MapProduct(p!))
            .ToList();

        return Ok(ordered);
    }

    [HttpGet("trending")]
    public async Task<ActionResult<List<RecentlyViewedProductDto>>> GetTrending()
    {
        var products = await _context.Products
            .AsNoTracking()
            .Include(p => p.Reviews)
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.Reviews.Average(r => (double?)r.Rating) ?? 0)
            .ThenByDescending(p => p.Reviews.Count)
            .Take(10)
            .ToListAsync();

        return Ok(products.Select(MapProduct).ToList());
    }

    private static RecentlyViewedProductDto MapProduct(Product p)
    {
        return new RecentlyViewedProductDto
        {
            ProductId = p.Id,
            Name = p.Name,
            ImageUrl = p.ImageUrl,
            Price = p.Price,
            OriginalPrice = p.OriginalPrice,
            DiscountPercent = p.DiscountPercent,
            Category = p.Category,
            AverageRating = p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 0,
            TotalReviews = p.Reviews.Count
        };
    }
}
