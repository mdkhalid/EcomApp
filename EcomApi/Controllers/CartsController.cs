using EcomApi.DTOs;
using EcomApi.Models;
using EcomApi.Repositories;
using Mapster;
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

    private string GetSessionId()
    {
        if (Request.Cookies.TryGetValue("CartId", out var sessionId) && !string.IsNullOrEmpty(sessionId))
            return sessionId;

        sessionId = Guid.NewGuid().ToString();
        Response.Cookies.Append("CartId", sessionId, new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            Expires = DateTime.UtcNow.AddDays(30),
            SameSite = SameSiteMode.Lax
        });
        return sessionId;
    }

    [HttpGet]
    public async Task<ActionResult<CartDto>> GetCart()
    {
        var sessionId = GetSessionId();
        var cart = await _repository.GetBySessionIdAsync(sessionId);

        if (cart == null)
        {
            cart = await _repository.CreateAsync(sessionId);
        }

        return Ok(cart.Adapt<CartDto>());
    }

    [HttpPost("items")]
    public async Task<ActionResult<CartDto>> AddItem([FromBody] AddCartItemDto dto)
    {
        var sessionId = GetSessionId();
        var cart = await _repository.AddItemAsync(sessionId, dto.ProductId, dto.Quantity);

        if (cart == null)
        {
            return BadRequest(new { error = "Product not found or insufficient stock." });
        }

        return Ok(cart.Adapt<CartDto>());
    }

    [HttpPut("items/{cartItemId}")]
    public async Task<ActionResult<CartDto>> UpdateItem(int cartItemId, [FromBody] UpdateCartItemDto dto)
    {
        var sessionId = GetSessionId();
        var cart = await _repository.UpdateItemAsync(cartItemId, dto.Quantity);

        if (cart == null)
        {
            return BadRequest(new { error = "Invalid quantity or product out of stock." });
        }

        return Ok(cart.Adapt<CartDto>());
    }

    [HttpDelete("items/{cartItemId}")]
    public async Task<ActionResult<CartDto>> RemoveItem(int cartItemId)
    {
        var sessionId = GetSessionId();
        var cart = await _repository.RemoveItemAsync(cartItemId);

        if (cart == null)
        {
            return NotFound();
        }

        return Ok(cart.Adapt<CartDto>());
    }

    [HttpDelete]
    public async Task<ActionResult<CartDto>> ClearCart()
    {
        var sessionId = GetSessionId();
        var cart = await _repository.ClearAsync(sessionId);
        return Ok(cart.Adapt<CartDto>());
    }
}