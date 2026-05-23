using EcomApi.Models;

namespace EcomApi.Repositories;

public interface IBannerRepository
{
    Task<IEnumerable<Banner>> GetAllAsync();
    Task<IEnumerable<Banner>> GetActiveAsync();
    Task<Banner?> GetByIdAsync(int id);
    Task<Banner> AddAsync(Banner banner);
    Task<Banner> UpdateAsync(Banner banner);
    Task<bool> DeleteAsync(int id);
}
