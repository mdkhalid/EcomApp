namespace EcomApi.Models;

public enum NotificationType
{
    Welcome,
    OrderConfirmation,
    OrderShipped,
    OrderDelivered,
    OrderStatusUpdate,
    TrackingUpdate,
    PasswordReset,
    BackInStock
}

public class NotificationMessage
{
    public NotificationType Type { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Subject { get; set; }
    public string? HtmlBody { get; set; }
    public string? TextBody { get; set; }

    // In-app (admin) notification fields
    public string? AdminMessage { get; set; }
    public string? AdminType { get; set; }
    public int? OrderId { get; set; }
}
