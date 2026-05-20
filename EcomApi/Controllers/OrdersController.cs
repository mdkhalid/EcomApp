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
public class OrdersController : ControllerBase
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICartRepository _cartRepository;

    public OrdersController(IOrderRepository orderRepository, ICartRepository cartRepository)
    {
        _orderRepository = orderRepository;
        _cartRepository = cartRepository;
    }

    private string GetUserIdOrSession()
    {
        var role = User.FindFirstValue(ClaimTypes.Role);
        if (role == "Admin")
            throw new UnauthorizedAccessException("Admin accounts cannot place orders.");

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

    [HttpPost]
    public async Task<ActionResult<OrderDto>> CreateOrder([FromBody] CreateOrderDto createDto)
    {
        var identifier = GetUserIdOrSession();
        var order = await _orderRepository.CreateFromCartAsync(identifier, createDto);

        if (order == null)
            return BadRequest(new { error = "Cart is empty. Add items before placing an order." });

        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, MapOrder(order));
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrders(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (items, totalCount) = await _orderRepository.GetByUserIdAsync(int.Parse(userId), pageNumber, pageSize);
        return Ok(new
        {
            items = items.Select(MapOrder),
            totalCount,
            pageNumber,
            pageSize
        });
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<OrderDto>> GetOrder(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var order = await _orderRepository.GetByIdAsync(id);

        if (order == null || (order.UserId != null && order.UserId != userId))
            return NotFound();

        return Ok(MapOrder(order));
    }

    [HttpPut("{id}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<OrderDto>> UpdateStatus(int id, [FromBody] UpdateOrderStatusDto dto)
    {
        if (!Enum.TryParse<OrderStatus>(dto.Status, true, out var status))
            return BadRequest(new { error = $"Invalid status. Valid values: {string.Join(", ", Enum.GetNames<OrderStatus>())}" });

        var order = await _orderRepository.UpdateStatusAsync(id, status);
        if (order == null)
            return NotFound();

        return Ok(MapOrder(order));
    }

    [HttpGet("all")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> GetAllOrders(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var (items, totalCount) = await _orderRepository.GetAllAsync(pageNumber, pageSize, status);

        return Ok(new
        {
            items = items.Select(MapOrder),
            totalCount,
            pageNumber,
            pageSize
        });
    }

    private static OrderDto MapOrder(Order order)
    {
        return new OrderDto
        {
            Id = order.Id,
            Status = order.Status.ToString(),
            ShippingName = order.ShippingName,
            ShippingAddress = order.ShippingAddress,
            ShippingCity = order.ShippingCity,
            ShippingZip = order.ShippingZip,
            TotalAmount = order.TotalAmount,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            Items = order.Items.Select(i => new OrderItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                ProductImage = i.ProductImage,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                TotalPrice = i.TotalPrice
            }).ToList()
        };
    }
}
