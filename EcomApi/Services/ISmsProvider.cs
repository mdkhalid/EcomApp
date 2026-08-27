namespace EcomApi.Services;

public interface ISmsProvider
{
    Task SendSmsAsync(string phone, string message, CancellationToken cancellationToken = default);
}

/// <summary>
/// Inert placeholder. Swap for a real provider (Twilio, etc.) later without
/// touching NotificationService or any controller.
/// </summary>
public class NullSmsProvider : ISmsProvider
{
    private readonly ILogger<NullSmsProvider> _logger;
    public NullSmsProvider(ILogger<NullSmsProvider> logger) => _logger = logger;
    public Task SendSmsAsync(string phone, string message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("SMS channel not configured — skipping message to {Phone}: {Message}", phone, message);
        return Task.CompletedTask;
    }
}
