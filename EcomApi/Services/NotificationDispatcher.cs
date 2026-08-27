using EcomApi.Models;
using EcomApi.Services.NotificationChannels;
using Microsoft.Extensions.Logging;

namespace EcomApi.Services;

/// <summary>
/// Delivers a queued notification across all enabled channels (Strategy pattern).
/// Resolved per-scope inside the background worker so channel dependencies (DbContext, etc.) stay scoped.
/// </summary>
public interface INotificationDispatcher
{
    Task DispatchAsync(NotificationMessage message, CancellationToken cancellationToken = default);
}

public sealed class NotificationDispatcher : INotificationDispatcher
{
    private readonly IEnumerable<INotificationChannel> _channels;
    private readonly ISettingsProvider _settings;
    private readonly ILogger<NotificationDispatcher> _logger;

    public NotificationDispatcher(
        IEnumerable<INotificationChannel> channels,
        ISettingsProvider settings,
        ILogger<NotificationDispatcher> logger)
    {
        _channels = channels;
        _settings = settings;
        _logger = logger;
    }

    public async Task DispatchAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        foreach (var channel in _channels)
        {
            var enabled = channel.ChannelType switch
            {
                NotificationChannelType.Email => await _settings.GetAsync("Notification:Email:Enabled", true, cancellationToken),
                NotificationChannelType.Sms => await _settings.GetAsync("Notification:Sms:Enabled", false, cancellationToken),
                NotificationChannelType.WhatsApp => await _settings.GetAsync("Notification:WhatsApp:Enabled", false, cancellationToken),
                _ => true
            };

            if (!enabled) continue;

            try
            {
                await channel.SendAsync(message, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Notification channel {Channel} failed for message type {Type}.",
                    channel.ChannelType, message.Type);
            }
        }
    }
}
