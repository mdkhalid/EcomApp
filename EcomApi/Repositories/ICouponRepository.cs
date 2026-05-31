using EcomApi.DTOs;
using EcomApi.Models;

namespace EcomApi.Repositories;

public interface ICouponRepository
{
    Task<IEnumerable<Coupon>> GetAllAsync();
    Task<Coupon?> GetByIdAsync(int id);
    Task<Coupon?> GetByCodeAsync(string code);
    Task<Coupon> AddAsync(Coupon coupon);
    Task<Coupon> UpdateAsync(Coupon coupon);
    Task<bool> DeleteAsync(int id);
    Task<ValidateCouponResponse> ValidateAndCalculateAsync(string code, decimal cartTotal, int? userId);
}
