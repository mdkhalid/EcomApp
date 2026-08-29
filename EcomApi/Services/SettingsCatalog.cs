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
        new("Auth:Enforce2FA", "Security", "Require 2FA for Admin/SubAdmin logins", false, "false")
    };

    public static SettingDescriptor? Find(string key) => All.FirstOrDefault(x => x.Key == key);
}
