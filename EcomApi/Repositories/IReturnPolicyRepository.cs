using EcomApi.Models;

namespace EcomApi.Repositories;

public interface IReturnPolicyRepository
{
    Task<ReturnPolicy?> GetAsync();
    Task<ReturnPolicy> CreateOrUpdateAsync(ReturnPolicy policy);
}
