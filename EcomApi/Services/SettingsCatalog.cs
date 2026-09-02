namespace EcomApi.Services;

public record SettingDescriptor(
    string Key,
    string Group,
    string Description,
    bool IsSensitive,
    string DefaultValue);

/// <summary>
/// The known, admin-editable settings. New configurable items are added here
/// only — no controller or service changes required elsewhere (Open/Closed).
/// </summary>
public static class SettingsCatalog
{
    public static readonly IReadOnlyList<SettingDescriptor> All = new List<SettingDescriptor>
    {
        new("Smtp:Host", "Email", "SMTP server hostname", false, ""),
        new("Smtp:Port", "Email", "SMTP port (usually 587 or 465)", false, "587"),
        new("Smtp:User", "Email", "SMTP username / API key", false, ""),
        new("Smtp:Password", "Email", "SMTP password / secret", true, ""),
        new("Smtp:From", "Email", "From address", false, ""),
        new("Smtp:FromName", "Email", "From display name", false, "Ecom"),
        new("Smtp:EnableSsl", "Email", "Use SSL/TLS", false, "true"),

        new("Notification:Email:Enabled", "Notifications", "Send emails", false, "true"),
        new("Notification:Sms:Enabled", "Notifications", "Send SMS", false, "false"),
        new("Notification:WhatsApp:Enabled", "Notifications", "Send WhatsApp", false, "false"),

        new("Client:BaseUrl", "General", "Frontend base URL (for email links)", false, "http://localhost:4200"),
        new("Cors:AllowedOrigins", "Security", "Comma-separated allowed CORS origins (requires restart)", false, "http://localhost:4200"),
        new("Auth:Enforce2FA", "Security", "Require 2FA for Admin/SubAdmin logins", false, "false"),
        new("Stripe:SecretKey", "Payments", "Stripe secret API key (empty = Mock gateway)", true, ""),
        new("Stripe:PublishableKey", "Payments", "Stripe publishable key (frontend)", false, ""),
        new("Stripe:WebhookSecret", "Payments", "Stripe webhook signing secret", true, ""),
        new("Shipping:DefaultRate", "Payments", "Fallback shipping fee when no zone matches", false, "49"),

        new("Cart:AbandonmentHours", "Cart", "Hours of cart inactivity before triggering a recovery email", false, "24"),
        new("Cart:RecoveryCouponEnabled", "Cart", "Attach a one-time coupon to abandoned-cart emails", false, "false"),
        new("Cart:RecoveryCouponAmount", "Cart", "Flat discount (₹) attached to each abandoned-cart recovery email", false, "100"),
        new("Cart:ResendDays", "Cart", "Cooldown days between abandoned-cart emails per user", false, "7"),
        new("Cart:ScanIntervalMinutes", "Cart", "How often the background worker scans for abandoned carts", false, "60"),
        new("Cart:MinCartTotal", "Cart", "Minimum cart subtotal required to send a recovery email", false, "100"),

        new("Digest:Enabled", "Digest", "Enable daily admin digest email", false, "false"),
        new("Digest:Time", "Digest", "Local time to send daily digest (HH:mm 24h)", false, "23:00"),
        new("Digest:Recipient", "Digest", "Admin email recipient for daily digest", false, ""),
        new("Digest:LowStockThreshold", "Digest", "Low-stock threshold for products included in digest", false, "10")
    };

    public static SettingDescriptor? Find(string key) => All.FirstOrDefault(x => x.Key == key);
}
