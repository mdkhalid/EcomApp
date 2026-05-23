using EcomApi.Models;

namespace EcomApi.Repositories;

public interface IWishlistRepository
{
    Task<IEnumerable<WishlistItem>> GetByUserIdAsync(int userId);
    Task<WishlistItem?> GetByUserAndProductAsync(int userId, int productId);
    Task<bool> IsWishlistedAsync(int userId, int productId);
    Task<WishlistItem> AddAsync(WishlistItem item);
    Task<bool> RemoveAsync(int id);
    Task RemoveByUserAndProductAsync(int userId, int productId);
}
