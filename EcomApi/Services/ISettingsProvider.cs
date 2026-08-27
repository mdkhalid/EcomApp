using EcomApi.Repositories;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace EcomApi.Services;

/// <summary>
/// Resolves configuration values at runtime: DB-backed settings first,
/// falling back to appsettings / environment variables. Results are cached
/// briefly so admin changes propagate within seconds without a restart.
/// This is the single source of truth for anything an admin can configure.
/// </summary>
public interface ISettingsProvider
{
    Task<string?> GetRawAsync(string key, CancellationToken cancellationToken = default);
    Task<T> GetAsync<T>(string key, T defaultValue, CancellationToken cancellationToken = default);
    Task UpsertAsync(string key, string? value, CancellationToken cancellationToken = default);
    Task InvalidateAsync(string key, CancellationToken cancellationToken = default);
}

public class SettingsProvider : ISettingsProvider
{
    private readonly ISettingRepository _repository;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;

    public SettingsProvider(ISettingRepository repository, IConfiguration configuration, IMemoryCache cache)
    {
        _repository = repository;
        _configuration = configuration;
        _cache = cache;
    }

    private string CacheKey(string key) => $"setting:{key}";

    public async Task<string?> GetRawAsync(string key, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue<string>(CacheKey(key), out var cached))
            return cached;

        var fromDb = await _repository.GetByKeyAsync(key, cancellationToken);
        // An explicitly-empty DB value (row exists, Value=="") is returned as-is and does NOT
        // fall back to configuration. Only a missing DB row falls through to appsettings/env.
        var value = fromDb?.Value ?? _configuration[key];

        _cache.Set(CacheKey(key), value, new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(1)
        });
        return value;
    }

    public async Task<T> GetAsync<T>(string key, T defaultValue, CancellationToken cancellationToken = default)
    {
        var raw = await GetRawAsync(key, cancellationToken);
        if (string.IsNullOrWhiteSpace(raw))
            return defaultValue;

        try
        {
            return (T)Convert.ChangeType(raw, typeof(T));
        }
        catch
        {
            return defaultValue;
        }
    }

    public async Task InvalidateAsync(string key, CancellationToken cancellationToken = default)
    {
        _cache.Remove(CacheKey(key));
        await Task.CompletedTask;
    }

    public async Task UpsertAsync(string key, string? value, CancellationToken cancellationToken = default)
    {
        await _repository.UpsertAsync(key, value, cancellationToken);
        _cache.Remove(CacheKey(key));
    }
}
