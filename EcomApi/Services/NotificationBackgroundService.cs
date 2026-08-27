using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EcomApi.Services;

/// <summary>
/// Drains the notification queue on a background thread. Each message is dispatched
/// within its own DI scope so scoped services (DbContext, settings) are correctly resolved.
/// </summary>
public sealed class NotificationBackgroundService : BackgroundService
{
    private readonly INotificationQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationBackgroundService> _logger;

    public NotificationBackgroundService(
        INotificationQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<NotificationBackgroundService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dispatcher = scope.ServiceProvider.GetRequiredService<INotificationDispatcher>();
                await dispatcher.DispatchAsync(message, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to dispatch notification of type {Type}.", message.Type);
            }
        }
    }
}
