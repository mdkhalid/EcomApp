using EcomApi.Models;

namespace EcomApi.Repositories;

public interface IReviewRepository
{
    Task<(IEnumerable<Review> Items, int TotalCount)> GetByProductIdAsync(int productId, int pageNumber, int pageSize);
    Task<(double AverageRating, int TotalReviews)> GetProductRatingAsync(int productId);
    Task<Dictionary<int, (double AverageRating, int TotalReviews)>> GetRatingsForProductsAsync(List<int> productIds);
    Task<Review?> GetByIdAsync(int id);
    Task<Review?> GetByUserAndProductAsync(int userId, int productId);
    Task<Review> CreateAsync(Review review);
    Task<bool> DeleteAsync(int id);
}
