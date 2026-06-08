using EcomApi.Models;

namespace EcomApi.Repositories;

public interface IActivityRepository
{
    Task LogAsync(UserActivity activity, CancellationToken cancellationToken = default);
    Task<List<UserActivity>> GetRecentByUserAsync(int userId, ActivityType? type = null, int limit = 20, CancellationToken cancellationToken = default);
    Task<List<UserActivity>> GetRecentBySessionAsync(string sessionId, ActivityType? type = null, int limit = 20, CancellationToken cancellationToken = default);
    Task<List<int>> GetRecentProductIdsAsync(int userId, string? sessionId, int limit = 20, CancellationToken cancellationToken = default);
    Task<List<int>> GetRecommendedProductIdsAsync(int userId, int limit = 10, CancellationToken cancellationToken = default);
    Task<List<int>> GetAlsoBoughtProductIdsAsync(int productId, int limit = 10, CancellationToken cancellationToken = default);
    Task<List<int>> GetFrequentlyBoughtTogetherAsync(int productId, int limit = 6, CancellationToken cancellationToken = default);
}
