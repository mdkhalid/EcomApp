using EcomApi.Models;

namespace EcomApi.Services;

public interface INotificationService
{
    Task SendOrderStatusUpdateAsync(Order order, OrderStatus previousStatus);
    Task SendOrderConfirmationAsync(Order order);
    Task SendTrackingUpdateAsync(Order order);
}

public class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(ILogger<NotificationService> logger)
    {
        _logger = logger;
    }

    public async Task SendOrderStatusUpdateAsync(Order order, OrderStatus previousStatus)
    {
        _logger.LogInformation("Order {OrderId} status changed from {Previous} to {Current}",
            order.Id, previousStatus, order.Status);

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

    // private string GetStatusSmsMessage(Order order, OrderStatus previousStatus)
    // {
    //     return $"Order #{order.Id}: Status updated to {order.Status}. " +
    //            (order.EstimatedDeliveryDate.HasValue
    //                ? $"Expected delivery: {order.EstimatedDeliveryDate:dd MMM}."
    //                : "");
    // }
}
