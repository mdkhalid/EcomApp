using System.Security.Claims;
using EcomApi.DTOs;
using EcomApi.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcomApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WishlistController : ControllerBase
{
    private readonly IWishlistRepository _repository;

    public WishlistController(IWishlistRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult> GetWishlist()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var items = await _repository.GetByUserIdAsync(userId);

        return Ok(new
        {
            items = items.Select(i => new WishlistItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.Product.Name,
                ProductImage = i.Product.ImageUrl,
                ProductPrice = i.Product.Price,
                Category = i.Product.Category,
                CreatedAt = i.CreatedAt
            }),
            totalCount = items.Count()
        });
    }

    [HttpGet("check/{productId}")]
    public async Task<ActionResult> CheckWishlisted(int productId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var isWishlisted = await _repository.IsWishlistedAsync(userId, productId);
        return Ok(new { isWishlisted });
    }

    [HttpPost("products/{productId}")]
    public async Task<ActionResult> Toggle(int productId)
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
    public async Task<ActionResult> Remove(int id)
    {
        var deleted = await _repository.RemoveAsync(id);
        if (!deleted)
            return NotFound();
        return NoContent();
    }
}
