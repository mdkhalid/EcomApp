using EcomApi.Models;

namespace EcomApi.Services;

public enum NotificationChannelType
{
    InApp,
    Email,
    Sms,
    WhatsApp
}

public interface INotificationChannel
{
    NotificationChannelType ChannelType { get; }
    Task SendAsync(NotificationMessage message, CancellationToken cancellationToken = default);
}
