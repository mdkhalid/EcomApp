using EcomApi.Models;

namespace EcomApi.Repositories;

public interface IActivityRepository
{
    Task LogAsync(UserActivity activity);
    Task<List<UserActivity>> GetRecentByUserAsync(int userId, ActivityType? type = null, int limit = 20);
    Task<List<UserActivity>> GetRecentBySessionAsync(string sessionId, ActivityType? type = null, int limit = 20);
    Task<List<int>> GetRecentProductIdsAsync(int userId, string? sessionId, int limit = 20);
    Task<List<int>> GetRecommendedProductIdsAsync(int userId, int limit = 10);
    Task<List<int>> GetAlsoBoughtProductIdsAsync(int productId, int limit = 10);
    Task<List<int>> GetFrequentlyBoughtTogetherAsync(int productId, int limit = 6);
}
