using System.Security.Claims;
using EcomApi.DTOs;
using EcomApi.Models;
using EcomApi.Repositories;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcomApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CartsController : ControllerBase
{
    private readonly ICartRepository _repository;
    private readonly IProductRepository _productRepository;

    public CartsController(ICartRepository repository, IProductRepository productRepository)
    {
        _repository = repository;
        _productRepository = productRepository;
    }

    private string GetUserIdOrSession()
    {
        var role = User.FindFirstValue(ClaimTypes.Role);
        if (role == "Admin")
            throw new UnauthorizedAccessException("Admin accounts cannot use shopping carts.");

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userIdClaim))
            return $"user:{userIdClaim}";

        if (Request.Cookies.TryGetValue("CartId", out var sessionId) && !string.IsNullOrEmpty(sessionId))
            return $"session:{sessionId}";

        sessionId = Guid.NewGuid().ToString();
        Response.Cookies.Append("CartId", sessionId, new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            Expires = DateTime.UtcNow.AddDays(30),
            SameSite = SameSiteMode.Lax
        });
        return $"session:{sessionId}";
    }

    [HttpGet]
    public async Task<ActionResult<CartDto>> GetCart(CancellationToken cancellationToken = default)
    {
        var identifier = GetUserIdOrSession();
        var cart = await _repository.GetByIdentifierAsync(identifier);

        if (cart == null)
        {
            cart = await _repository.CreateAsync(identifier);
        }

        return Ok(cart.Adapt<CartDto>());
    }

    [HttpPost("items")]
    public async Task<ActionResult<CartDto>> AddItem([FromBody] AddCartItemDto dto, CancellationToken cancellationToken = default)
    {
        var identifier = GetUserIdOrSession();
        var cart = await _repository.AddItemAsync(identifier, dto.ProductId, dto.Quantity, dto.ProductVariantId);

        if (cart == null)
        {
            return BadRequest(new { error = "Product not found or insufficient stock." });
        }

        return Ok(cart.Adapt<CartDto>());
    }

    [HttpPut("items/{cartItemId}")]
    public async Task<ActionResult<CartDto>> UpdateItem(int cartItemId, [FromBody] UpdateCartItemDto dto, CancellationToken cancellationToken = default)
    {
        var identifier = GetUserIdOrSession();
        var cart = await _repository.UpdateItemAsync(cartItemId, dto.Quantity);

        if (cart == null)
        {
            return BadRequest(new { error = "Invalid quantity or product out of stock." });
        }

        return Ok(cart.Adapt<CartDto>());
    }

    [HttpDelete("items/{cartItemId}")]
    public async Task<ActionResult<CartDto>> RemoveItem(int cartItemId, CancellationToken cancellationToken = default)
    {
        var identifier = GetUserIdOrSession();
        var cart = await _repository.RemoveItemAsync(cartItemId);

        if (cart == null)
        {
            return NotFound();
        }

        return Ok(cart.Adapt<CartDto>());
    }

    [HttpDelete]
    public async Task<ActionResult<CartDto>> ClearCart(CancellationToken cancellationToken = default)
    {
        var identifier = GetUserIdOrSession();
        var cart = await _repository.ClearAsync(identifier);
        return Ok(cart.Adapt<CartDto>());
    }

    [Authorize]
    [HttpPost("merge")]
    public async Task<IActionResult> MergeSessionCart(CancellationToken cancellationToken = default)
    {
        if (!Request.Cookies.TryGetValue("CartId", out var sessionId) || string.IsNullOrEmpty(sessionId))
            return Ok(new { message = "No session cart to merge." });

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var userIdentifier = $"user:{userId}";
        var sessionIdentifier = $"session:{sessionId}";

        await _repository.MergeCartsAsync(sessionIdentifier, userIdentifier);
        Response.Cookies.Delete("CartId");

        return Ok(new { message = "Cart merged successfully." });
    }
}
