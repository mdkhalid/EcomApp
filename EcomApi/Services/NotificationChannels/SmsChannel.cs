using EcomApi.Models;
using EcomApi.Services;

namespace EcomApi.Services.NotificationChannels;

public class SmsChannel : INotificationChannel
{
    private readonly ISmsProvider _smsProvider;
    public SmsChannel(ISmsProvider smsProvider) => _smsProvider = smsProvider;
    public NotificationChannelType ChannelType => NotificationChannelType.Sms;

    public async Task SendAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message.Phone) || string.IsNullOrWhiteSpace(message.TextBody)) return;
        await _smsProvider.SendSmsAsync(message.Phone, message.TextBody, cancellationToken);
    }
}
