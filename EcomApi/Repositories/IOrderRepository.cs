using EcomApi.DTOs;
using EcomApi.Models;

namespace EcomApi.Repositories;

public interface IOrderRepository
{
    Task<Order?> CreateFromCartAsync(string sessionId, CreateOrderDto createDto);
    Task<IEnumerable<Order>> GetBySessionIdAsync(string sessionId);
    Task<Order?> GetByIdAsync(int orderId);
    Task<Order?> UpdateStatusAsync(int orderId, OrderStatus status);
}
