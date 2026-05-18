using EcomApi.DTOs;
using EcomApi.Models;
using EcomApi.Repositories;
using Mapster;
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

    [HttpPost]
    public async Task<ActionResult<OrderDto>> CreateOrder([FromBody] CreateOrderDto createDto)
    {
        var sessionId = GetSessionId();
        var order = await _orderRepository.CreateFromCartAsync(sessionId, createDto);

        if (order == null)
            return BadRequest(new { error = "Cart is empty. Add items before placing an order." });

        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, MapOrder(order));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrders()
    {
        var sessionId = GetSessionId();
        var orders = await _orderRepository.GetBySessionIdAsync(sessionId);
        return Ok(orders.Select(MapOrder));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrderDto>> GetOrder(int id)
    {
        var sessionId = GetSessionId();
        var order = await _orderRepository.GetByIdAsync(id);

        if (order == null || order.SessionId != sessionId)
            return NotFound();

        return Ok(MapOrder(order));
    }

    [HttpPut("{id}/status")]
    public async Task<ActionResult<OrderDto>> UpdateStatus(int id, [FromBody] UpdateOrderStatusDto dto)
    {
        if (!Enum.TryParse<OrderStatus>(dto.Status, true, out var status))
            return BadRequest(new { error = $"Invalid status. Valid values: {string.Join(", ", Enum.GetNames<OrderStatus>())}" });

        var order = await _orderRepository.UpdateStatusAsync(id, status);
        if (order == null)
            return NotFound();

        return Ok(MapOrder(order));
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
