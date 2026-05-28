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

        var estimatedDelivery = CalculateEstimatedDelivery(DateTime.UtcNow);

        var order = new Order
        {
            UserId = cart.UserId,
            SessionId = cart.SessionId,
            Status = OrderStatus.Pending,
            ShippingName = createDto.ShippingName,
            ShippingAddress = createDto.ShippingAddress,
            ShippingCity = createDto.ShippingCity,
            ShippingZip = createDto.ShippingZip,
            CustomerEmail = createDto.CustomerEmail,
            CustomerPhone = createDto.CustomerPhone,
            EstimatedDeliveryDate = estimatedDelivery,
            Items = cart.Items.Select(ci => new OrderItem
            {
                ProductId = ci.ProductId,
                ProductName = ci.Product.Name,
                ProductImage = ci.Product.ImageUrl,
                Quantity = ci.Quantity,
                UnitPrice = ci.UnitPrice,
                TotalPrice = ci.TotalPrice
            }).ToList(),
            StatusHistory = new List<OrderStatusHistory>
            {
                new OrderStatusHistory
                {
                    Status = OrderStatus.Pending,
                    Note = "Order placed successfully",
                    Location = "Online",
                    CreatedAt = DateTime.UtcNow
                }
            },
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
        return await GetWithHistoryAsync(order.Id);
    }

    public async Task<IEnumerable<Order>> GetBySessionIdAsync(string sessionId)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .Include(o => o.StatusHistory)
            .Where(o => o.SessionId == sessionId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<(IEnumerable<Order> Items, int TotalCount)> GetByUserIdAsync(int userId, int pageNumber, int pageSize)
    {
        var query = _context.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .Include(o => o.StatusHistory)
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
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.Id == orderId);
    }

    public async Task<Order?> GetWithHistoryAsync(int orderId)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .Include(o => o.StatusHistory.OrderByDescending(h => h.CreatedAt))
            .FirstOrDefaultAsync(o => o.Id == orderId);
    }

    public async Task<Order?> UpdateStatusAsync(int orderId, OrderStatus status, string? note = null, string? location = null)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null)
            return null;

        var previousStatus = order.Status;
        order.Status = status;
        order.UpdatedAt = DateTime.UtcNow;

        if (status == OrderStatus.Delivered)
        {
            order.ActualDeliveryDate = DateTime.UtcNow;
        }

        // Add status history
        var historyEntry = new OrderStatusHistory
        {
            OrderId = orderId,
            Status = status,
            Note = note ?? GetDefaultNote(status),
            Location = location,
            CreatedAt = DateTime.UtcNow
        };
        _context.OrderStatusHistories.Add(historyEntry);

        await _context.SaveChangesAsync();
        return await GetWithHistoryAsync(orderId);
    }

    public async Task<Order?> UpdateTrackingAsync(int orderId, string trackingNumber, string carrier, DateTime? estimatedDeliveryDate)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null)
            return null;

        order.TrackingNumber = trackingNumber;
        order.Carrier = carrier;
        if (estimatedDeliveryDate.HasValue)
            order.EstimatedDeliveryDate = estimatedDeliveryDate.Value;
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return await GetWithHistoryAsync(orderId);
    }

    public async Task<(IEnumerable<Order> Items, int TotalCount)> GetAllAsync(int pageNumber, int pageSize, string? status = null)
    {
        var query = _context.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .Include(o => o.StatusHistory)
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

    public async Task<List<ShippingAddressDto>> GetPreviousAddressesAsync(int userId)
    {
        return await _context.Orders
            .AsNoTracking()
            .Where(o => o.UserId == userId)
            .Select(o => new ShippingAddressDto
            {
                Name = o.ShippingName,
                Address = o.ShippingAddress,
                City = o.ShippingCity,
                Zip = o.ShippingZip
            })
            .Distinct()
            .Take(10)
            .ToListAsync();
    }

    public async Task<bool> HasUserPurchasedProductAsync(int userId, int productId)
    {
        return await _context.Orders
            .Where(o => o.UserId == userId && o.Status == OrderStatus.Delivered)
            .AnyAsync(o => o.Items.Any(i => i.ProductId == productId));
    }

    private static DateTime CalculateEstimatedDelivery(DateTime orderDate)
    {
        // Add 5-7 business days for delivery
        var deliveryDays = 5;
        var estimated = orderDate;
        var daysAdded = 0;

        while (daysAdded < deliveryDays)
        {
            estimated = estimated.AddDays(1);
            if (estimated.DayOfWeek != DayOfWeek.Saturday && estimated.DayOfWeek != DayOfWeek.Sunday)
            {
                daysAdded++;
            }
        }

        return estimated;
    }

    private static string GetDefaultNote(OrderStatus status)
    {
        return status switch
        {
            OrderStatus.Pending => "Order placed and awaiting confirmation",
            OrderStatus.Processing => "Order is being prepared",
            OrderStatus.Shipped => "Order has been shipped",
            OrderStatus.OutForDelivery => "Order is out for delivery",
            OrderStatus.Delivered => "Order has been delivered",
            OrderStatus.Cancelled => "Order has been cancelled",
            OrderStatus.Returned => "Order has been returned",
            _ => ""
        };
    }
}
