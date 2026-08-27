using System.Threading.Channels;
using EcomApi.Models;

namespace EcomApi.Services;

/// <summary>
/// Decouples notification production (request thread) from delivery (background worker)
/// so HTTP responses never block on SMTP / SMS / WhatsApp I/O.
/// </summary>
public interface INotificationQueue
{
    Task EnqueueAsync(NotificationMessage message, CancellationToken cancellationToken = default);
    ChannelReader<NotificationMessage> Reader { get; }
}

public sealed class NotificationQueue : INotificationQueue
{
    private readonly Channel<NotificationMessage> _channel =
        Channel.CreateUnbounded<NotificationMessage>(new UnboundedChannelOptions { SingleReader = false });

    public ChannelReader<NotificationMessage> Reader => _channel.Reader;

    public Task EnqueueAsync(NotificationMessage message, CancellationToken cancellationToken = default)
        => _channel.Writer.WriteAsync(message, cancellationToken).AsTask();
}
