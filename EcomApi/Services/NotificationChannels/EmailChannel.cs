using EcomApi.Models;
using EcomApi.Services;

namespace EcomApi.Services.NotificationChannels;

public class EmailChannel : INotificationChannel
{
    private readonly IEmailService _emailService;

    public EmailChannel(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public NotificationChannelType ChannelType => NotificationChannelType.Email;

    public async Task SendAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message.Email) || string.IsNullOrWhiteSpace(message.HtmlBody)) return;
        await _emailService.SendAsync(message.Email, message.Subject ?? "", message.HtmlBody, cancellationToken);
    }
}
