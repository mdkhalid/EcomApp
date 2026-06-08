using EcomApi.Models;

namespace EcomApi.Repositories;

public interface IWishlistRepository
{
    Task<IEnumerable<WishlistItem>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<WishlistItem?> GetByUserAndProductAsync(int userId, int productId, CancellationToken cancellationToken = default);
    Task<bool> IsWishlistedAsync(int userId, int productId, CancellationToken cancellationToken = default);
    Task<WishlistItem> AddAsync(WishlistItem item, CancellationToken cancellationToken = default);
    Task<bool> RemoveAsync(int id, CancellationToken cancellationToken = default);
    Task RemoveByUserAndProductAsync(int userId, int productId, CancellationToken cancellationToken = default);
}
