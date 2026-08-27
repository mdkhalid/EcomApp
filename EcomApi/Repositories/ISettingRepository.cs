using EcomApi.Data;
using EcomApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EcomApi.Repositories;

public interface ISettingRepository
{
    Task<Setting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Setting>> GetAllAsync(CancellationToken cancellationToken = default);
    Task UpsertAsync(string key, string? value, CancellationToken cancellationToken = default);
}

public class SettingRepository : ISettingRepository
{
    private readonly ApplicationDbContext _context;
    public SettingRepository(ApplicationDbContext context) => _context = context;

    public async Task<Setting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
        => await _context.Settings.FirstOrDefaultAsync(s => s.Key == key, cancellationToken);

    public async Task<IReadOnlyList<Setting>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _context.Settings.AsNoTracking().ToListAsync(cancellationToken);

    public async Task UpsertAsync(string key, string? value, CancellationToken cancellationToken = default)
    {
        var existing = await _context.Settings.FirstOrDefaultAsync(s => s.Key == key, cancellationToken);
        if (existing == null)
        {
            _context.Settings.Add(new Setting { Key = key, Value = value });
        }
        else
        {
            existing.Value = value;
        }
        await _context.SaveChangesAsync(cancellationToken);
    }
}
