using EcomApi.Data;
using EcomApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EcomApi.Services.NotificationChannels;

public class InAppChannel : INotificationChannel
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<InAppChannel> _logger;

    public InAppChannel(ApplicationDbContext context, ILogger<InAppChannel> logger)
    {
        _context = context;
        _logger = logger;
    }

    public NotificationChannelType ChannelType => NotificationChannelType.InApp;

    public async Task SendAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message.AdminMessage)) return;

        _context.AdminNotifications.Add(new AdminNotification
        {
            Message = message.AdminMessage,
            Type = message.AdminType ?? "info",
            OrderId = message.OrderId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("In-app admin notification created: {Message}", message.AdminMessage);
    }
}
