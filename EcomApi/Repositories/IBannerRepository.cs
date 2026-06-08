using EcomApi.Models;

namespace EcomApi.Repositories;

public interface IBannerRepository
{
    Task<IEnumerable<Banner>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Banner>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<Banner?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Banner> AddAsync(Banner banner, CancellationToken cancellationToken = default);
    Task<Banner> UpdateAsync(Banner banner, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
