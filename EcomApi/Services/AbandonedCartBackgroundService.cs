using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EcomApi.Services;

/// <summary>
/// Periodically scans for abandoned carts and dispatches recovery emails via the
/// existing notification pipeline. The interval is admin-configurable via
/// <c>Cart:ScanIntervalMinutes</c> in <see cref="SettingsCatalog"/>. Each scan
/// runs in its own DI scope so scoped services (DbContext, settings, queue) are
/// resolved correctly.
/// </summary>
public sealed class AbandonedCartBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AbandonedCartBackgroundService> _logger;

    public AbandonedCartBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<AbandonedCartBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Small initial delay so app startup completes and migrations run before the first scan.
        try
        {
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
        catch (TaskCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            int intervalMinutes;
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var settings = scope.ServiceProvider.GetRequiredService<ISettingsProvider>();
                intervalMinutes = await settings.GetAsync("Cart:ScanIntervalMinutes", 60, stoppingToken);
            }
            catch
            {
                intervalMinutes = 60;
            }

            if (intervalMinutes < 1) intervalMinutes = 1;

            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(intervalMinutes));

            do
            {
                try
                {
                    using var scanScope = _scopeFactory.CreateScope();
                    var runner = scanScope.ServiceProvider.GetRequiredService<AbandonedCartScanRunner>();
                    var sent = await runner.RunAsync(stoppingToken);
                    if (sent > 0)
                        _logger.LogInformation("Abandoned-cart scan complete. {Sent} recovery email(s) queued.", sent);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { throw; }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Abandoned-cart scan failed; will retry next interval.");
                }

                try
                {
                    if (!await timer.WaitForNextTickAsync(stoppingToken)) break;
                }
                catch (OperationCanceledException) { return; }
            } while (!stoppingToken.IsCancellationRequested);
        }
    }
}