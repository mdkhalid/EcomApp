using EcomApi.DTOs;
using EcomApi.Models;

namespace EcomApi.Repositories;

public interface ICouponRepository
{
    Task<IEnumerable<Coupon>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Coupon?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Coupon?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<Coupon> AddAsync(Coupon coupon, CancellationToken cancellationToken = default);
    Task<Coupon> UpdateAsync(Coupon coupon, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<ValidateCouponResponse> ValidateAndCalculateAsync(string code, decimal cartTotal, int? userId, CancellationToken cancellationToken = default);
}
