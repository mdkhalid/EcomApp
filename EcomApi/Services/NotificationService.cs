using EcomApi.Data;
using EcomApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EcomApi.Services;

public interface INotificationService
{
    Task SendOrderStatusUpdateAsync(Order order, OrderStatus previousStatus);
    Task SendOrderConfirmationAsync(Order order);
    Task SendTrackingUpdateAsync(Order order);
    Task<int> GetUnreadNotificationCountAsync();
    Task<List<AdminNotification>> GetNotificationsAsync(int page = 1, int pageSize = 20);
    Task MarkNotificationReadAsync(int id);
    Task MarkAllNotificationsReadAsync();
}

public class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;
    private readonly ApplicationDbContext _context;

    public NotificationService(ILogger<NotificationService> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    private async Task CreateAdminNotification(string message, string type, int? orderId = null)
    {
        _context.AdminNotifications.Add(new AdminNotification
        {
            Message = message,
            Type = type,
            OrderId = orderId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
    }

    public async Task SendOrderStatusUpdateAsync(Order order, OrderStatus previousStatus)
    {
        _logger.LogInformation("Order {OrderId} status changed from {Previous} to {Current}",
            order.Id, previousStatus, order.Status);

        await CreateAdminNotification(
            $"Order #{order.Id} status updated from {previousStatus} to {order.Status}",
            "status_update", order.Id);

        // ============================================
        // SMS Notification ( uncomment when ready )
        // ============================================
        // if (!string.IsNullOrEmpty(order.CustomerPhone))
        // {
        //     var message = GetStatusSmsMessage(order, previousStatus);
        //     await _smsService.SendAsync(order.CustomerPhone, message);
        // }

        // ============================================
        // Email Notification ( uncomment when ready )
        // ============================================
        // if (!string.IsNullOrEmpty(order.CustomerEmail))
        // {
        //     var subject = $"Order #{order.Id} - Status Updated to {order.Status}";
        //     var body = GetStatusEmailBody(order, previousStatus);
        //     await _emailService.SendAsync(order.CustomerEmail, subject, body);
        // }

        await Task.CompletedTask;
    }

    public async Task SendOrderConfirmationAsync(Order order)
    {
        _logger.LogInformation("Order {OrderId} confirmation sent to {Email}",
            order.Id, order.CustomerEmail);

        await CreateAdminNotification(
            $"New order #{order.Id} placed — ₹{order.TotalAmount:N2} by {order.ShippingName}",
            "new_order", order.Id);

        // ============================================
        // Email Confirmation ( uncomment when ready )
        // ============================================
        // if (!string.IsNullOrEmpty(order.CustomerEmail))
        // {
        //     var subject = $"Order #{order.Id} Confirmed - Thank you for your purchase!";
        //     var body = GetConfirmationEmailBody(order);
        //     await _emailService.SendAsync(order.CustomerEmail, subject, body);
        // }

        await Task.CompletedTask;
    }

    public async Task SendTrackingUpdateAsync(Order order)
    {
        _logger.LogInformation("Tracking update for Order {OrderId}: {TrackingNumber} via {Carrier}",
            order.Id, order.TrackingNumber, order.Carrier);

        await CreateAdminNotification(
            $"Order #{order.Id} tracking updated — {order.Carrier}: {order.TrackingNumber}",
            "tracking_update", order.Id);

        // ============================================
        // SMS with Tracking ( uncomment when ready )
        // ============================================
        // if (!string.IsNullOrEmpty(order.CustomerPhone))
        // {
        //     var message = $"Your order #{order.Id} has been shipped! Track: {order.Carrier} - {order.TrackingNumber}";
        //     await _smsService.SendAsync(order.CustomerPhone, message);
        // }

        // ============================================
        // Email with Tracking ( uncomment when ready )
        // ============================================
        // if (!string.IsNullOrEmpty(order.CustomerEmail))
        // {
        //     var subject = $"Order #{order.Id} Shipped - Tracking Info";
        //     var body = GetTrackingEmailBody(order);
        //     await _emailService.SendAsync(order.CustomerEmail, subject, body);
        // }

        await Task.CompletedTask;
    }

    // ============================================
    // Email Templates ( uncomment when ready )
    // ============================================

    // private string GetConfirmationEmailBody(Order order)
    // {
    //     return $"""
    //         <h2>Order Confirmed!</h2>
    //         <p>Thank you for your order #{order.Id}.</p>
    //         <p><strong>Total:</strong> ₹{order.TotalAmount:N2}</p>
    //         <p><strong>Shipping to:</strong> {order.ShippingName}, {order.ShippingAddress}, {order.ShippingCity} {order.ShippingZip}</p>
    //         <p>We'll notify you when your order ships.</p>
    //     """;
    // }

    // private string GetStatusEmailBody(Order order, OrderStatus previousStatus)
    // {
    //     return $"""
    //         <h2>Order #{order.Id} Status Update</h2>
    //         <p>Your order status has been updated from <strong>{previousStatus}</strong> to <strong>{order.Status}</strong>.</p>
    //         {($"<p><strong>Estimated Delivery:</strong> {order.EstimatedDeliveryDate:dd MMM yyyy}</p>" if order.EstimatedDeliveryDate.HasValue else "")}
    //     """;
    // }

    // private string GetTrackingEmailBody(Order order)
    // {
    //     return $"""
    //         <h2>Your Order Has Shipped!</h2>
    //         <p>Order #{order.Id} has been shipped.</p>
    //         <p><strong>Carrier:</strong> {order.Carrier}</p>
    //         <p><strong>Tracking Number:</strong> {order.TrackingNumber}</p>
    //         {($"<p><strong>Estimated Delivery:</strong> {order.EstimatedDeliveryDate:dd MMM yyyy}</p>" if order.EstimatedDeliveryDate.HasValue else "")}
    //     """;
    // }

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

    // private string GetStatusSmsMessage(Order order, OrderStatus previousStatus)
    // {
    //     return $"Order #{order.Id}: Status updated to {order.Status}. " +
    //            (order.EstimatedDeliveryDate.HasValue
    //                ? $"Expected delivery: {order.EstimatedDeliveryDate:dd MMM}."
    //                : "");
    // }
}
