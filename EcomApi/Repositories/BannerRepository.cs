using EcomApi.Data;
using EcomApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EcomApi.Repositories;

public class BannerRepository : IBannerRepository
{
    private readonly ApplicationDbContext _context;

    public BannerRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Banner>> GetAllAsync()
    {
        return await _context.Banners.AsNoTracking().OrderBy(b => b.SortOrder).ThenBy(b => b.Id).ToListAsync();
    }

    public async Task<IEnumerable<Banner>> GetActiveAsync()
    {
        var now = DateTime.UtcNow;
        return await _context.Banners.AsNoTracking()
            .Where(b => b.IsActive && b.StartDate <= now && b.StartDate.AddDays(b.DurationDays) > now)
            .OrderBy(b => b.SortOrder)
            .ThenBy(b => b.Id)
            .ToListAsync();
    }

    public async Task<Banner?> GetByIdAsync(int id)
    {
        return await _context.Banners.FindAsync(id);
    }

    public async Task<Banner> AddAsync(Banner banner)
    {
        _context.Banners.Add(banner);
        await _context.SaveChangesAsync();
        return banner;
    }

    public async Task<Banner> UpdateAsync(Banner banner)
    {
        _context.Entry(banner).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return banner;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var banner = await _context.Banners.FindAsync(id);
        if (banner == null) return false;

        _context.Banners.Remove(banner);
        await _context.SaveChangesAsync();
        return true;
    }
}
