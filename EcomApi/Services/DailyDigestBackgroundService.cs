using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EcomApi.Services;

/// <summary>
/// Runs the daily digest at the configured local time (default 23:00).
/// The time is evaluated each day using the server's local timezone.
/// </summary>
public sealed class DailyDigestBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DailyDigestBackgroundService> _logger;

    public DailyDigestBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<DailyDigestBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken).ContinueWith(_ => { }, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            TimeSpan timeUntilNextRun;
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var settings = scope.ServiceProvider.GetRequiredService<ISettingsProvider>();
                var timeStr = await settings.GetRawAsync("Digest:Time", stoppingToken) ?? "23:00";
                if (!TimeSpan.TryParse(timeStr, out var parseTime))
                    parseTime = TimeSpan.FromHours(23);

                var now = DateTime.Now;
                var targetToday = now.Date + parseTime;
                if (targetToday <= now)
                    targetToday = targetToday.AddDays(1);

                timeUntilNextRun = targetToday - now;
            }
            catch
            {
                timeUntilNextRun = TimeSpan.FromHours(1);
            }

            if (timeUntilNextRun <= TimeSpan.Zero)
                timeUntilNextRun = TimeSpan.FromMinutes(1);

            _logger.LogDebug("Daily digest next run in {TimeUntilNextRun}.", timeUntilNextRun);

            try
            {
                await Task.Delay(timeUntilNextRun, stoppingToken);
            }
            catch (TaskCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (stoppingToken.IsCancellationRequested) return;

            try
            {
                using var runScope = _scopeFactory.CreateScope();
                var runner = runScope.ServiceProvider.GetRequiredService<DailyDigestRunner>();
                await runner.RunAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Daily digest execution failed; will retry tomorrow.");
            }
        }
    }
}