using EcomApi.Data;
using EcomApi.Models;
using EcomApi.Services.NotificationChannels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EcomApi.Services;

public interface INotificationService
{
    Task SendOrderStatusUpdateAsync(Order order, OrderStatus previousStatus);
    Task SendOrderConfirmationAsync(Order order);
    Task SendTrackingUpdateAsync(Order order);
    Task SendWelcomeAsync(User user);
    Task<int> GetUnreadNotificationCountAsync();
    Task<List<AdminNotification>> GetNotificationsAsync(int page = 1, int pageSize = 20);
    Task MarkNotificationReadAsync(int id);
    Task MarkAllNotificationsReadAsync();
}

public class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;
    private readonly ApplicationDbContext _context;
    private readonly IEnumerable<INotificationChannel> _channels;
    private readonly ISettingsProvider _settings;

    public NotificationService(
        ILogger<NotificationService> logger,
        ApplicationDbContext context,
        IEnumerable<INotificationChannel> channels,
        ISettingsProvider settings)
    {
        _logger = logger;
        _context = context;
        _channels = channels;
        _settings = settings;
    }

    private async Task Dispatch(NotificationMessage message, CancellationToken cancellationToken = default)
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

    public async Task SendOrderStatusUpdateAsync(Order order, OrderStatus previousStatus)
    {
        _logger.LogInformation("Order {OrderId} status changed from {Previous} to {Current}",
            order.Id, previousStatus, order.Status);

        var (subject, html) = order.Status switch
        {
            OrderStatus.Shipped => ($"Order #{order.Id} Shipped", EmailTemplates.OrderShipped(order)),
            OrderStatus.Delivered => ($"Order #{order.Id} Delivered", EmailTemplates.OrderDelivered(order)),
            _ => ($"Order #{order.Id} - Status Updated to {order.Status}", EmailTemplates.OrderStatusUpdate(order, previousStatus))
        };

        var message = new NotificationMessage
        {
            Type = NotificationType.OrderStatusUpdate,
            Email = order.CustomerEmail,
            Phone = order.CustomerPhone,
            Subject = subject,
            HtmlBody = html,
            TextBody = GetStatusSmsMessage(order, previousStatus),
            AdminMessage = $"Order #{order.Id} status updated from {previousStatus} to {order.Status}",
            AdminType = "status_update",
            OrderId = order.Id
        };

        await Dispatch(message);
    }

    public async Task SendOrderConfirmationAsync(Order order)
    {
        _logger.LogInformation("Order {OrderId} confirmation dispatched to {Email}",
            order.Id, order.CustomerEmail);

        var message = new NotificationMessage
        {
            Type = NotificationType.OrderConfirmation,
            Email = order.CustomerEmail,
            Phone = order.CustomerPhone,
            Subject = $"Order #{order.Id} Confirmed - Thank you for your purchase!",
            HtmlBody = EmailTemplates.OrderConfirmation(order),
            TextBody = $"Your order #{order.Id} for ₹{order.TotalAmount:N2} is confirmed. We'll notify you when it ships.",
            AdminMessage = $"New order #{order.Id} placed — ₹{order.TotalAmount:N2} by {order.ShippingName}",
            AdminType = "new_order",
            OrderId = order.Id
        };

        await Dispatch(message);
    }

    public async Task SendWelcomeAsync(User user)
    {
        _logger.LogInformation("Welcome email dispatched to {Email}", user.Email);

        var message = new NotificationMessage
        {
            Type = NotificationType.Welcome,
            Email = user.Email,
            Subject = "Welcome to Ecom!",
            HtmlBody = EmailTemplates.Welcome(user.FirstName ?? user.Username, user.Email),
            TextBody = $"Welcome to Ecom, {user.Username}!"
        };

        await Dispatch(message);
    }

    public async Task SendTrackingUpdateAsync(Order order)
    {
        _logger.LogInformation("Tracking update for Order {OrderId}: {TrackingNumber} via {Carrier}",
            order.Id, order.TrackingNumber, order.Carrier);

        var message = new NotificationMessage
        {
            Type = NotificationType.TrackingUpdate,
            Email = order.CustomerEmail,
            Phone = order.CustomerPhone,
            Subject = $"Order #{order.Id} Shipped - Tracking Info",
            HtmlBody = EmailTemplates.Tracking(order),
            TextBody = $"Your order #{order.Id} has been shipped! Track: {order.Carrier} - {order.TrackingNumber}",
            AdminMessage = $"Order #{order.Id} tracking updated — {order.Carrier}: {order.TrackingNumber}",
            AdminType = "tracking_update",
            OrderId = order.Id
        };

        await Dispatch(message);
    }

    private string GetStatusSmsMessage(Order order, OrderStatus previousStatus)
    {
        return $"Order #{order.Id}: Status updated to {order.Status}. " +
               (order.EstimatedDeliveryDate.HasValue
                   ? $"Expected delivery: {order.EstimatedDeliveryDate:dd MMM}."
                   : "");
    }

    public async Task<int> GetUnreadNotificationCountAsync()
    {
        return await _context.AdminNotifications.CountAsync(n => !n.IsRead);
    }

    public async Task<List<AdminNotification>> GetNotificationsAsync(int page = 1, int pageSize = 20)
    {
        return await _context.AdminNotifications
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task MarkNotificationReadAsync(int id)
    {
        var notification = await _context.AdminNotifications.FindAsync(id);
        if (notification != null)
        {
            notification.IsRead = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task MarkAllNotificationsReadAsync()
    {
        await _context.AdminNotifications
            .Where(n => !n.IsRead)
            .ExecuteUpdateAsync(setters => setters.SetProperty(n => n.IsRead, true));
    }
}
