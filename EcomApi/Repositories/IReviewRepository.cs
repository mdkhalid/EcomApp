using EcomApi.Models;

namespace EcomApi.Repositories;

public interface IReviewRepository
{
    Task<(IEnumerable<Review> Items, int TotalCount)> GetByProductIdAsync(int productId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<(double AverageRating, int TotalReviews)> GetProductRatingAsync(int productId, CancellationToken cancellationToken = default);
    Task<Dictionary<int, (double AverageRating, int TotalReviews)>> GetRatingsForProductsAsync(List<int> productIds, CancellationToken cancellationToken = default);
    Task<Review?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Review?> GetByUserAndProductAsync(int userId, int productId, CancellationToken cancellationToken = default);
    Task<Review> CreateAsync(Review review, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
