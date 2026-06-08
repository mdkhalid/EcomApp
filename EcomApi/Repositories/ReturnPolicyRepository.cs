using EcomApi.Data;
using EcomApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EcomApi.Repositories;

public class ReturnPolicyRepository : IReturnPolicyRepository
{
    private readonly ApplicationDbContext _context;

    public ReturnPolicyRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ReturnPolicy?> GetAsync()
    {
        return await _context.ReturnPolicies.FirstOrDefaultAsync();
    }

    public async Task<ReturnPolicy> CreateOrUpdateAsync(ReturnPolicy policy)
    {
        var existing = await _context.ReturnPolicies.FirstOrDefaultAsync();
        if (existing != null)
        {
            existing.ReturnWindowDays = policy.ReturnWindowDays;
            existing.IsActive = policy.IsActive;
            existing.PolicyText = policy.PolicyText;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedBy = policy.UpdatedBy;
        }
        else
        {
            policy.UpdatedAt = DateTime.UtcNow;
            _context.ReturnPolicies.Add(policy);
        }

        await _context.SaveChangesAsync();
        return existing ?? policy;
    }
}
