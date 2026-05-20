using EcomApi.Models;
using EcomApi.DTOs;

namespace EcomApi.Repositories;

public interface ICartRepository
{
    Task<Cart?> GetBySessionIdAsync(string sessionId);
    Task<Cart?> GetByIdentifierAsync(string identifier);
    Task<Cart> CreateAsync(string sessionId);
    Task<Cart?> AddItemAsync(string sessionId, int productId, int quantity);
    Task<Cart?> UpdateItemAsync(int cartItemId, int quantity);
    Task<Cart?> RemoveItemAsync(int cartItemId);
    Task<Cart> ClearAsync(string sessionId);
    Task<bool> ExistsAsync(int id);
    Task MergeCartsAsync(string sourceIdentifier, string targetIdentifier);
}
