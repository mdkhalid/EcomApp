namespace EcomApi.Services;

public interface IWhatsAppProvider
{
    Task SendWhatsAppAsync(string phone, string message, CancellationToken cancellationToken = default);
}

/// <summary>
/// Inert placeholder. Swap for a real provider (WhatsApp Business API, etc.) later
/// without touching NotificationService or any controller.
/// </summary>
public class NullWhatsAppProvider : IWhatsAppProvider
{
    private readonly ILogger<NullWhatsAppProvider> _logger;
    public NullWhatsAppProvider(ILogger<NullWhatsAppProvider> logger) => _logger = logger;
    public Task SendWhatsAppAsync(string phone, string message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("WhatsApp channel not configured — skipping message to {Phone}: {Message}", phone, message);
        return Task.CompletedTask;
    }
}
