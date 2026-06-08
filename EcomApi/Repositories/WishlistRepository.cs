using EcomApi.Data;
using EcomApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EcomApi.Repositories;

public class WishlistRepository : IWishlistRepository
{
    private readonly ApplicationDbContext _context;

    public WishlistRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<WishlistItem>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _context.WishlistItems.AsNoTracking()
            .Include(w => w.Product)
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<WishlistItem?> GetByUserAndProductAsync(int userId, int productId, CancellationToken cancellationToken = default)
    {
        return await _context.WishlistItems
            .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId, cancellationToken);
    }

    public async Task<bool> IsWishlistedAsync(int userId, int productId, CancellationToken cancellationToken = default)
    {
        return await _context.WishlistItems.AsNoTracking()
            .AnyAsync(w => w.UserId == userId && w.ProductId == productId, cancellationToken);
    }

    public async Task<WishlistItem> AddAsync(WishlistItem item, CancellationToken cancellationToken = default)
    {
        _context.WishlistItems.Add(item);
        await _context.SaveChangesAsync(cancellationToken);
        return item;
    }

    public async Task<bool> RemoveAsync(int id, CancellationToken cancellationToken = default)
    {
        var item = await _context.WishlistItems.FindAsync(id);
        if (item == null) return false;
        _context.WishlistItems.Remove(item);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task RemoveByUserAndProductAsync(int userId, int productId, CancellationToken cancellationToken = default)
    {
        var item = await _context.WishlistItems
            .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId, cancellationToken);
        if (item != null)
        {
            _context.WishlistItems.Remove(item);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
