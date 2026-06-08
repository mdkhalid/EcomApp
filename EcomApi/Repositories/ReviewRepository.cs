using EcomApi.Data;
using EcomApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EcomApi.Repositories;

public class ReviewRepository : IReviewRepository
{
    private readonly ApplicationDbContext _context;

    public ReviewRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<(IEnumerable<Review> Items, int TotalCount)> GetByProductIdAsync(int productId, int pageNumber, int pageSize)
    {
        var query = _context.Reviews.AsNoTracking()
            .Include(r => r.User)
            .Where(r => r.ProductId == productId)
            .OrderByDescending(r => r.CreatedAt);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<(double AverageRating, int TotalReviews)> GetProductRatingAsync(int productId)
    {
        var reviews = await _context.Reviews.AsNoTracking()
            .Where(r => r.ProductId == productId)
            .ToListAsync();

        if (reviews.Count == 0)
            return (0, 0);

        return (reviews.Average(r => r.Rating), reviews.Count);
    }

    public async Task<Dictionary<int, (double AverageRating, int TotalReviews)>> GetRatingsForProductsAsync(List<int> productIds)
    {
        var ratings = await _context.Reviews.AsNoTracking()
            .Where(r => productIds.Contains(r.ProductId))
            .GroupBy(r => r.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                AverageRating = g.Average(r => r.Rating),
                TotalReviews = g.Count()
            })
            .ToListAsync();

        var result = new Dictionary<int, (double, int)>();
        foreach (var id in productIds)
        {
            var rating = ratings.FirstOrDefault(r => r.ProductId == id);
            result[id] = rating != null
                ? (rating.AverageRating, rating.TotalReviews)
                : (0, 0);
        }
        return result;
    }

    public async Task<Review?> GetByIdAsync(int id)
    {
        return await _context.Reviews.FindAsync(id);
    }

    public async Task<Review?> GetByUserAndProductAsync(int userId, int productId)
    {
        return await _context.Reviews.AsNoTracking()
            .FirstOrDefaultAsync(r => r.UserId == userId && r.ProductId == productId);
    }

    public async Task<Review> CreateAsync(Review review)
    {
        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();
        return review;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var review = await _context.Reviews.FindAsync(id);
        if (review == null) return false;
        _context.Reviews.Remove(review);
        await _context.SaveChangesAsync();
        return true;
    }
}
