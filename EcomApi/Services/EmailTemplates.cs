using EcomApi.Models;

namespace EcomApi.Services;

/// <summary>
/// Centralized HTML email templates (Single Responsibility). Adding/editing a
/// template touches only this file. Each method returns a complete HTML document.
/// </summary>
public static class EmailTemplates
{
    private static string Wrap(string title, string body) => $"""
        <!DOCTYPE html>
        <html lang="en">
        <head><meta charset="utf-8" /><meta name="viewport" content="width=device-width, initial-scale=1" />
        <title>{title}</title></head>
        <body style="margin:0;padding:0;background:#f4f4f5;font-family:Arial,Helvetica,sans-serif;color:#18181b;">
          <table role="presentation" width="100%" cellpadding="0" cellspacing="0">
            <tr><td align="center" style="padding:24px;">
              <table role="presentation" width="600" cellpadding="0" cellspacing="0" style="background:#ffffff;border-radius:10px;overflow:hidden;box-shadow:0 1px 3px rgba(0,0,0,.1);">
                <tr><td style="background:#4f46e5;padding:20px 24px;color:#fff;font-size:20px;font-weight:bold;">Ecom</td></tr>
                <tr><td style="padding:24px;">{body}</td></tr>
                <tr><td style="padding:16px 24px;background:#fafafa;color:#71717a;font-size:12px;">
                  You received this email from Ecom. If you no longer wish to receive these, you can ignore this message.
                </td></tr>
              </table>
            </td></tr>
          </table>
        </body>
        </html>
        """;

    public static string Welcome(string name, string email) => Wrap("Welcome to Ecom", $"""
        <h2>Welcome{(!string.IsNullOrWhiteSpace(name) ? $", {name}" : "")}! 🎉</h2>
        <p>Thanks for creating an account with <strong>Ecom</strong> using <strong>{email}</strong>.</p>
        <p>Start exploring our products and enjoy a smooth shopping experience.</p>
        """);

    public static string OrderConfirmation(Order order) => Wrap($"Order #{order.Id} Confirmed", $"""
        <h2>Order Confirmed!</h2>
        <p>Thank you for your order <strong>#{order.Id}</strong>.</p>
        <p><strong>Total:</strong> ₹{order.TotalAmount:N2}</p>
        <p><strong>Shipping to:</strong> {order.ShippingName}, {order.ShippingAddress}, {order.ShippingCity} {order.ShippingZip}</p>
        <p>We'll notify you when your order ships.</p>
        """);

    public static string OrderStatusUpdate(Order order, OrderStatus previousStatus) => Wrap($"Order #{order.Id} Update", $"""
        <h2>Order #{order.Id} Status Update</h2>
        <p>Your order status has been updated from <strong>{previousStatus}</strong> to <strong>{order.Status}</strong>.</p>
        {EstimatedDelivery(order)}
        """);

    public static string OrderShipped(Order order) => Wrap($"Order #{order.Id} Shipped", $"""
        <h2>Your Order Has Shipped! 🚚</h2>
        <p>Order <strong>#{order.Id}</strong> is on its way.</p>
        <p><strong>Carrier:</strong> {order.Carrier}</p>
        <p><strong>Tracking Number:</strong> {order.TrackingNumber}</p>
        {EstimatedDelivery(order)}
        """);

    public static string OrderDelivered(Order order) => Wrap($"Order #{order.Id} Delivered", $"""
        <h2>Order Delivered ✅</h2>
        <p>Your order <strong>#{order.Id}</strong> has been delivered. We hope you love it!</p>
        """);

    public static string Tracking(Order order) => Wrap($"Order #{order.Id} Tracking", $"""
        <h2>Tracking Information</h2>
        <p>Order <strong>#{order.Id}</strong> has been shipped.</p>
        <p><strong>Carrier:</strong> {order.Carrier}</p>
        <p><strong>Tracking Number:</strong> {order.TrackingNumber}</p>
        {EstimatedDelivery(order)}
        """);

    public static string PasswordReset(string name, string resetLink) => Wrap("Reset your password", $"""
        <h2>Password Reset Request</h2>
        <p>Hi {name}, we received a request to reset your password.</p>
        <p><a href="{resetLink}" style="display:inline-block;background:#4f46e5;color:#fff;padding:12px 20px;border-radius:6px;text-decoration:none;font-weight:bold;">Reset Password</a></p>
        <p>This link expires in 30 minutes. If you didn't request this, you can safely ignore this email.</p>
        """);

    private static string EstimatedDelivery(Order order) =>
        order.EstimatedDeliveryDate.HasValue
            ? $"<p><strong>Estimated Delivery:</strong> {order.EstimatedDeliveryDate:dd MMM yyyy}</p>"
            : "";
}
