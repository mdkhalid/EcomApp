using EcomApi.Models;

namespace EcomApi.Repositories;

public interface IReturnPolicyRepository
{
    Task<ReturnPolicy?> GetAsync(CancellationToken cancellationToken = default);
    Task<ReturnPolicy> CreateOrUpdateAsync(ReturnPolicy policy, CancellationToken cancellationToken = default);
}
