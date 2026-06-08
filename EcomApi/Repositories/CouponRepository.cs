using EcomApi.Data;
using EcomApi.DTOs;
using EcomApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EcomApi.Repositories;

public class CouponRepository : ICouponRepository
{
    private readonly ApplicationDbContext _context;

    public CouponRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Coupon>> GetAllAsync()
    {
        return await _context.Coupons.AsNoTracking().OrderByDescending(c => c.CreatedAt).ToListAsync();
    }

    public async Task<Coupon?> GetByIdAsync(int id)
    {
        return await _context.Coupons.FindAsync(id);
    }

    public async Task<Coupon?> GetByCodeAsync(string code)
    {
        return await _context.Coupons.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Code.ToUpper() == code.ToUpper());
    }

    public async Task<Coupon> AddAsync(Coupon coupon)
    {
        _context.Coupons.Add(coupon);
        await _context.SaveChangesAsync();
        return coupon;
    }

    public async Task<Coupon> UpdateAsync(Coupon coupon)
    {
        _context.Entry(coupon).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return coupon;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var coupon = await _context.Coupons.FindAsync(id);
        if (coupon == null) return false;

        _context.Coupons.Remove(coupon);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<ValidateCouponResponse> ValidateAndCalculateAsync(string code, decimal cartTotal, int? userId)
    {
        var coupon = await _context.Coupons.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Code.ToUpper() == code.ToUpper());

        if (coupon == null)
            return new ValidateCouponResponse { IsValid = false, ErrorMessage = "Invalid coupon code." };

        if (!coupon.IsActive)
            return new ValidateCouponResponse { IsValid = false, ErrorMessage = "This coupon is no longer active." };

        if (coupon.ExpiresAt < DateTime.UtcNow)
            return new ValidateCouponResponse { IsValid = false, ErrorMessage = "This coupon has expired." };

        if (coupon.MaxUses > 0 && coupon.CurrentUses >= coupon.MaxUses)
            return new ValidateCouponResponse { IsValid = false, ErrorMessage = "This coupon has reached its usage limit." };

        if (cartTotal < coupon.MinCartValue)
            return new ValidateCouponResponse
            {
                IsValid = false,
                ErrorMessage = $"Minimum cart value of ₹{coupon.MinCartValue} required for this coupon."
            };

        if (userId.HasValue)
        {
            var usageCount = await _context.CouponUsages
                .CountAsync(u => u.CouponId == coupon.Id && u.UserId == userId.Value);
            if (coupon.MaxUses > 0 && usageCount >= coupon.MaxUses)
                return new ValidateCouponResponse
                {
                    IsValid = false,
                    ErrorMessage = "You have already used this coupon the maximum number of times."
                };
        }

        decimal discount = coupon.Type == CouponType.Percentage
            ? Math.Round(cartTotal * coupon.Value / 100m, 2)
            : coupon.Value;

        if (discount > cartTotal) discount = cartTotal;

        return new ValidateCouponResponse
        {
            IsValid = true,
            Code = coupon.Code,
            Description = coupon.Description,
            Type = coupon.Type.ToString(),
            DiscountAmount = discount,
            FinalTotal = cartTotal - discount
        };
    }
}
