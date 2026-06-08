using EcomApi.Models;
using EcomApi.DTOs;

namespace EcomApi.Repositories;

public interface ICartRepository
{
    Task<Cart?> GetBySessionIdAsync(string sessionId, CancellationToken cancellationToken = default);
    Task<Cart?> GetByIdentifierAsync(string identifier, CancellationToken cancellationToken = default);
    Task<Cart> CreateAsync(string sessionId, CancellationToken cancellationToken = default);
    Task<Cart?> AddItemAsync(string sessionId, int productId, int quantity, int? productVariantId = null, CancellationToken cancellationToken = default);
    Task<Cart?> UpdateItemAsync(int cartItemId, int quantity, CancellationToken cancellationToken = default);
    Task<Cart?> RemoveItemAsync(int cartItemId, CancellationToken cancellationToken = default);
    Task<Cart> ClearAsync(string sessionId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
    Task MergeCartsAsync(string sourceIdentifier, string targetIdentifier, CancellationToken cancellationToken = default);
}
