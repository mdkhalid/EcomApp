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

    public async Task<Order?> CreateFromCartAsync(string identifier, CreateOrderDto createDto)
    {
        Cart? cart;

        if (identifier.StartsWith("user:"))
        {
            var userId = int.Parse(identifier.Substring(5));
            cart = await _context.Carts
                .Include(c => c.Items)
                .ThenInclude(ci => ci.Product)
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.UpdatedAt)
                .FirstOrDefaultAsync();
        }
        else
        {
            var sessionId = identifier.StartsWith("session:") ? identifier.Substring(8) : identifier;
            cart = await _context.Carts
                .Include(c => c.Items)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.SessionId == sessionId);
        }

        if (cart == null || cart.Items.Count == 0)
            return null;

        var order = new Order
        {
            UserId = cart.UserId,
            SessionId = cart.SessionId,
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

    public async Task<(IEnumerable<Order> Items, int TotalCount)> GetByUserIdAsync(int userId, int pageNumber, int pageSize)
    {
        var query = _context.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
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

    public async Task<(IEnumerable<Order> Items, int TotalCount)> GetAllAsync(int pageNumber, int pageSize, string? status = null)
    {
        var query = _context.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OrderStatus>(status, true, out var orderStatus))
        {
            query = query.Where(o => o.Status == orderStatus);
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}
