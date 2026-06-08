using EcomApi.Models;

namespace EcomApi.Repositories;

public interface IReturnRepository
{
    Task<ReturnRequest?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<ReturnRequest>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<ReturnRequest?> GetByOrderIdAsync(int orderId, int userId, CancellationToken cancellationToken = default);
    Task<bool> HasPendingReturnAsync(int orderId, int userId, CancellationToken cancellationToken = default);
    Task<ReturnRequest> CreateAsync(ReturnRequest returnRequest, CancellationToken cancellationToken = default);
    Task<ReturnRequest?> UpdateStatusAsync(int id, ReturnStatus status, string? adminNote = null, CancellationToken cancellationToken = default);
    Task<(List<ReturnRequest> Items, int TotalCount)> GetAllAsync(int pageNumber, int pageSize, string? status = null, CancellationToken cancellationToken = default);
}
