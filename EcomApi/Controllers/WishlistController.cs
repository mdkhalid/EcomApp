using System.Security.Claims;
using EcomApi.Data;
using EcomApi.DTOs;
using EcomApi.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcomApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WishlistController : ControllerBase
{
    private readonly IWishlistRepository _repository;
    private readonly ApplicationDbContext _context;

    public WishlistController(IWishlistRepository repository, ApplicationDbContext context)
    {
        _repository = repository;
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult> GetWishlist(CancellationToken cancellationToken = default)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var items = await _repository.GetByUserIdAsync(userId);

        var productIds = items.Select(i => i.ProductId).ToList();
        var firstImages = await _context.ProductImages
            .Where(pi => productIds.Contains(pi.ProductId))
            .GroupBy(pi => pi.ProductId)
            .Select(g => new { ProductId = g.Key, ImageUrl = g.OrderBy(pi => pi.SortOrder).Select(pi => pi.ImageUrl).FirstOrDefault() })
            .ToDictionaryAsync(x => x.ProductId, x => x.ImageUrl);

        return Ok(new
        {
            items = items.Select(i => new WishlistItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.Product.Name,
                ProductImage = firstImages.GetValueOrDefault(i.ProductId) ?? i.Product.ProductImages.OrderBy(pi => pi.SortOrder).Select(pi => pi.ImageUrl).FirstOrDefault(),
                ProductPrice = i.Product.Price,
                Category = i.Product.Category,
                CreatedAt = i.CreatedAt
            }),
            totalCount = items.Count()
        });
    }

    [HttpGet("check/{productId}")]
    public async Task<ActionResult> CheckWishlisted(int productId, CancellationToken cancellationToken = default)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var isWishlisted = await _repository.IsWishlistedAsync(userId, productId);
        return Ok(new { isWishlisted });
    }

    [HttpPost("products/{productId}")]
    public async Task<ActionResult> Toggle(int productId, CancellationToken cancellationToken = default)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var existing = await _repository.GetByUserAndProductAsync(userId, productId);
        if (existing != null)
        {
            await _repository.RemoveAsync(existing.Id);
            return Ok(new { wishlisted = false, message = "Removed from wishlist" });
        }

        var item = new EcomApi.Models.WishlistItem
        {
            UserId = userId,
            ProductId = productId
        };

        await _repository.AddAsync(item);
        return Ok(new { wishlisted = true, message = "Added to wishlist" });
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Remove(int id, CancellationToken cancellationToken = default)
    {
        var deleted = await _repository.RemoveAsync(id);
        if (!deleted)
            return NotFound();
        return NoContent();
    }
}
