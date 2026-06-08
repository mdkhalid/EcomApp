using EcomApi.DTOs;
using EcomApi.Models;

namespace EcomApi.Repositories;

public interface IOrderRepository
{
    Task<Order?> CreateFromCartAsync(string identifier, CreateOrderDto createDto, CancellationToken cancellationToken = default);
    Task<IEnumerable<Order>> GetBySessionIdAsync(string sessionId, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Order> Items, int TotalCount)> GetByUserIdAsync(int userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<Order?> GetByIdAsync(int orderId, CancellationToken cancellationToken = default);
    Task<Order?> UpdateStatusAsync(int orderId, OrderStatus status, string? note = null, string? location = null, CancellationToken cancellationToken = default);
    Task<Order?> UpdateTrackingAsync(int orderId, string trackingNumber, string carrier, DateTime? estimatedDeliveryDate, CancellationToken cancellationToken = default);
    Task<Order?> GetWithHistoryAsync(int orderId, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Order> Items, int TotalCount)> GetAllAsync(int pageNumber, int pageSize, string? status = null, CancellationToken cancellationToken = default);
    Task<bool> HasUserPurchasedProductAsync(int userId, int productId, CancellationToken cancellationToken = default);
    Task<List<ShippingAddressDto>> GetPreviousAddressesAsync(int userId, CancellationToken cancellationToken = default);
}
