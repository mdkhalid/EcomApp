using EcomApi.Models;
using EcomApi.Services;

namespace EcomApi.Services.NotificationChannels;

public class WhatsAppChannel : INotificationChannel
{
    private readonly IWhatsAppProvider _whatsAppProvider;
    public WhatsAppChannel(IWhatsAppProvider whatsAppProvider) => _whatsAppProvider = whatsAppProvider;
    public NotificationChannelType ChannelType => NotificationChannelType.WhatsApp;

    public async Task SendAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message.Phone) || string.IsNullOrWhiteSpace(message.TextBody)) return;
        await _whatsAppProvider.SendWhatsAppAsync(message.Phone, message.TextBody, cancellationToken);
    }
}
