using EcomApi.Data;
using EcomApi.DTOs;
using EcomApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EcomApi.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly ApplicationDbContext _context;

    public OrderRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Order?> CreateFromCartAsync(string sessionId, CreateOrderDto createDto)
    {
        var cart = await _context.Carts
            .Include(c => c.Items)
            .ThenInclude(ci => ci.Product)
            .FirstOrDefaultAsync(c => c.SessionId == sessionId);

        if (cart == null || cart.Items.Count == 0)
            return null;

        var order = new Order
        {
            SessionId = sessionId,
            Status = OrderStatus.Pending,
            ShippingName = createDto.ShippingName,
            ShippingAddress = createDto.ShippingAddress,
            ShippingCity = createDto.ShippingCity,
            ShippingZip = createDto.ShippingZip,
            Items = cart.Items.Select(ci => new OrderItem
            {
                ProductId = ci.ProductId,
                ProductName = ci.Product.Name,
                ProductImage = ci.Product.ImageUrl,
                Quantity = ci.Quantity,
                UnitPrice = ci.UnitPrice,
                TotalPrice = ci.TotalPrice
            }).ToList(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        order.TotalAmount = order.Items.Sum(i => i.TotalPrice);

        foreach (var item in cart.Items)
        {
            item.Product.Stock -= item.Quantity;
        }

        _context.Orders.Add(order);
        _context.CartItems.RemoveRange(cart.Items);
        _context.Carts.Remove(cart);

        await _context.SaveChangesAsync();
        return await GetByIdAsync(order.Id);
    }

    public async Task<IEnumerable<Order>> GetBySessionIdAsync(string sessionId)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .Where(o => o.SessionId == sessionId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<Order?> GetByIdAsync(int orderId)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId);
    }

    public async Task<Order?> UpdateStatusAsync(int orderId, OrderStatus status)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null)
            return null;

        order.Status = status;
        order.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return await GetByIdAsync(orderId);
    }
}
