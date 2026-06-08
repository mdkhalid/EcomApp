using EcomApi.Data;
using EcomApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EcomApi.Repositories;

public class ActivityRepository : IActivityRepository
{
    private readonly ApplicationDbContext _context;

    public ActivityRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(UserActivity activity, CancellationToken cancellationToken = default)
    {
        _context.UserActivities.Add(activity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<UserActivity>> GetRecentByUserAsync(int userId, ActivityType? type = null, int limit = 20, CancellationToken cancellationToken = default)
    {
        var query = _context.UserActivities
            .AsNoTracking()
            .Where(a => a.UserId == userId);

        if (type.HasValue)
            query = query.Where(a => a.Type == type.Value);

        return await query
            .OrderByDescending(a => a.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<UserActivity>> GetRecentBySessionAsync(string sessionId, ActivityType? type = null, int limit = 20, CancellationToken cancellationToken = default)
    {
        var query = _context.UserActivities
            .AsNoTracking()
            .Where(a => a.SessionId == sessionId);

        if (type.HasValue)
            query = query.Where(a => a.Type == type.Value);

        return await query
            .OrderByDescending(a => a.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<int>> GetRecentProductIdsAsync(int userId, string? sessionId, int limit = 20, CancellationToken cancellationToken = default)
    {
        var productIds = new List<int>();

        if (userId > 0)
        {
            var ids = await _context.UserActivities
                .AsNoTracking()
                .Where(a => a.UserId == userId && a.Type == ActivityType.ProductView)
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => a.Data)
                .Take(limit)
                .ToListAsync(cancellationToken);

            productIds.AddRange(ids.Where(id => int.TryParse(id, out _)).Select(int.Parse));
        }

        if (!string.IsNullOrEmpty(sessionId))
        {
            var sessionIds = await _context.UserActivities
                .AsNoTracking()
                .Where(a => a.SessionId == sessionId && a.Type == ActivityType.ProductView)
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => a.Data)
                .Take(limit)
                .ToListAsync(cancellationToken);

            productIds.AddRange(sessionIds.Where(id => int.TryParse(id, out _)).Select(int.Parse));
        }

        return productIds.Distinct().Take(limit).ToList();
    }

    public async Task<List<int>> GetRecommendedProductIdsAsync(int userId, int limit = 10, CancellationToken cancellationToken = default)
    {
        var viewedCategories = await _context.UserActivities
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => a.Data)
            .Take(50)
            .ToListAsync(cancellationToken);

        var productIds = viewedCategories
            .Where(id => int.TryParse(id, out _))
            .Select(int.Parse)
            .ToList();

        if (productIds.Count == 0)
            return new List<int>();

        var categories = await _context.Products
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id) && p.IsActive)
            .Select(p => p.Category)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (categories.Count == 0)
            return new List<int>();

        var recommended = await _context.Products
            .AsNoTracking()
            .Where(p => categories.Contains(p.Category) && p.IsActive && !productIds.Contains(p.Id))
            .OrderByDescending(p => p.Reviews.Average(r => (double?)r.Rating) ?? 0)
            .ThenByDescending(p => p.Reviews.Count)
            .Take(limit)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        return recommended;
    }

    public async Task<List<int>> GetAlsoBoughtProductIdsAsync(int productId, int limit = 10, CancellationToken cancellationToken = default)
    {
        var orderIds = await _context.OrderItems
            .AsNoTracking()
            .Where(oi => oi.ProductId == productId)
            .Select(oi => oi.OrderId)
            .Distinct()
            .Take(100)
            .ToListAsync(cancellationToken);

        if (orderIds.Count == 0)
            return new List<int>();

        var alsoBought = await _context.OrderItems
            .AsNoTracking()
            .Where(oi => orderIds.Contains(oi.OrderId) && oi.ProductId != productId)
            .GroupBy(oi => oi.ProductId)
            .Select(g => new { ProductId = g.Key, Count = g.Sum(oi => oi.Quantity) })
            .OrderByDescending(x => x.Count)
            .Take(limit)
            .Select(x => x.ProductId)
            .ToListAsync(cancellationToken);

        return alsoBought;
    }

    public async Task<List<int>> GetFrequentlyBoughtTogetherAsync(int productId, int limit = 6, CancellationToken cancellationToken = default)
    {
        var orderIds = await _context.OrderItems
            .AsNoTracking()
            .Where(oi => oi.ProductId == productId)
            .Select(oi => oi.OrderId)
            .Distinct()
            .Take(100)
            .ToListAsync(cancellationToken);

        if (orderIds.Count == 0)
            return new List<int>();

        var pairCounts = await _context.OrderItems
            .AsNoTracking()
            .Where(oi => orderIds.Contains(oi.OrderId) && oi.ProductId != productId)
            .GroupBy(oi => oi.ProductId)
            .Select(g => new { ProductId = g.Key, Count = g.Select(oi => oi.OrderId).Distinct().Count() })
            .OrderByDescending(x => x.Count)
            .Take(limit)
            .Select(x => x.ProductId)
            .ToListAsync(cancellationToken);

        return pairCounts;
    }
}
