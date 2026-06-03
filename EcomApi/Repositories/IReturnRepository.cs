using EcomApi.Models;

namespace EcomApi.Repositories;

public interface IReturnRepository
{
    Task<ReturnRequest?> GetByIdAsync(int id);
    Task<List<ReturnRequest>> GetByUserIdAsync(int userId);
    Task<ReturnRequest?> GetByOrderIdAsync(int orderId, int userId);
    Task<bool> HasPendingReturnAsync(int orderId, int userId);
    Task<ReturnRequest> CreateAsync(ReturnRequest returnRequest);
    Task<ReturnRequest?> UpdateStatusAsync(int id, ReturnStatus status, string? adminNote = null);
    Task<(List<ReturnRequest> Items, int TotalCount)> GetAllAsync(int pageNumber, int pageSize, string? status = null);
}
