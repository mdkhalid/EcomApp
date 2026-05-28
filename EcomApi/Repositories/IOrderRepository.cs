using EcomApi.DTOs;
using EcomApi.Models;

namespace EcomApi.Repositories;

public interface IOrderRepository
{
    Task<Order?> CreateFromCartAsync(string identifier, CreateOrderDto createDto);
    Task<IEnumerable<Order>> GetBySessionIdAsync(string sessionId);
    Task<(IEnumerable<Order> Items, int TotalCount)> GetByUserIdAsync(int userId, int pageNumber, int pageSize);
    Task<Order?> GetByIdAsync(int orderId);
    Task<Order?> UpdateStatusAsync(int orderId, OrderStatus status, string? note = null, string? location = null);
    Task<Order?> UpdateTrackingAsync(int orderId, string trackingNumber, string carrier, DateTime? estimatedDeliveryDate);
    Task<Order?> GetWithHistoryAsync(int orderId);
    Task<(IEnumerable<Order> Items, int TotalCount)> GetAllAsync(int pageNumber, int pageSize, string? status = null);
    Task<bool> HasUserPurchasedProductAsync(int userId, int productId);
    Task<List<ShippingAddressDto>> GetPreviousAddressesAsync(int userId);
}
