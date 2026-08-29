using EcomApi.Models;

namespace EcomApi.Services;

public record CreatePaymentRequest(
    int OrderId,
    decimal Amount,
    string Currency,
    string IdempotencyKey,
    string? CustomerEmail);

public record PaymentIntentResult(
    bool Success,
    string? GatewayPaymentId = null,
    string? ClientSecret = null,
    string? Error = null);

public record RefundResult(
    bool Success,
    string? GatewayRefundId = null,
    string? Error = null);

public record WebhookEventResult(
    string EventId,
    string EventType,
    int? OrderId,
    bool Succeeded,
    bool Refunded,
    decimal? RefundAmount,
    string? GatewayPaymentId = null);

/// <summary>
/// Gateway-agnostic payment contract (Strategy pattern, mirrors INotificationChannel).
/// A new provider is added by implementing this interface only.
/// </summary>
public interface IPaymentGateway
{
    PaymentGateway Gateway { get; }
    Task<PaymentIntentResult> CreatePaymentIntentAsync(CreatePaymentRequest request, CancellationToken cancellationToken = default);
    Task<RefundResult> RefundAsync(string gatewayPaymentId, decimal amount, string reason, CancellationToken cancellationToken = default);
    /// <summary>Verifies the signature and normalizes the event. Returns null if invalid.</summary>
    Task<WebhookEventResult?> ParseWebhookAsync(string body, string signatureHeader, CancellationToken cancellationToken = default);
}
