using EcomApi.Models;

namespace EcomApi.Services;

/// <summary>
/// In-memory / dev gateway used when no Stripe keys are configured. Lets the full
/// money-movement flow run end-to-end (create intent → confirm → order Paid) without
/// real credentials. Falls back to this automatically via <see cref="PaymentGatewayFactory"/>.
/// </summary>
public class MockGateway : IPaymentGateway
{
    public PaymentGateway Gateway => PaymentGateway.Mock;

    public Task<PaymentIntentResult> CreatePaymentIntentAsync(CreatePaymentRequest request, CancellationToken cancellationToken = default)
    {
        var id = "mock_pi_" + Guid.NewGuid().ToString("N")[..16];
        var clientSecret = id + "_secret";
        return Task.FromResult(new PaymentIntentResult(true, id, clientSecret));
    }

    public Task<RefundResult> RefundAsync(string gatewayPaymentId, decimal amount, string reason, CancellationToken cancellationToken = default)
    {
        var refundId = "mock_re_" + Guid.NewGuid().ToString("N")[..12];
        return Task.FromResult(new RefundResult(true, refundId));
    }

    public Task<WebhookEventResult?> ParseWebhookAsync(string body, string signatureHeader, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<WebhookEventResult?>(null);
    }
}
