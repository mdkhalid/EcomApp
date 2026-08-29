using EcomApi.Models;
using Microsoft.Extensions.Configuration;
using Stripe;
using PaymentIntentService = Stripe.PaymentIntentService;
using RefundService = Stripe.RefundService;

namespace EcomApi.Services;

/// <summary>
/// Stripe implementation of <see cref="IPaymentGateway"/>. Uses Stripe.net.
/// Secrets come from configuration / env (Stripe:SecretKey, Stripe:WebhookSecret).
/// </summary>
public class StripeGateway : IPaymentGateway
{
    private readonly string? _webhookSecret;

    public StripeGateway(IConfiguration configuration)
    {
        StripeConfiguration.ApiKey = configuration["Stripe:SecretKey"];
        _webhookSecret = configuration["Stripe:WebhookSecret"];
    }

    public PaymentGateway Gateway => PaymentGateway.Stripe;

    public async Task<PaymentIntentResult> CreatePaymentIntentAsync(CreatePaymentRequest request, CancellationToken cancellationToken = default)
    {
        var service = new PaymentIntentService();
        var options = new PaymentIntentCreateOptions
        {
            Amount = (long)(request.Amount * 100), // minor units (paise for INR)
            Currency = request.Currency.ToLowerInvariant(),
            ReceiptEmail = request.CustomerEmail,
            Metadata = new Dictionary<string, string> { ["orderId"] = request.OrderId.ToString() },
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions { Enabled = true }
        };

        try
        {
            var intent = await service.CreateAsync(options, new RequestOptions { IdempotencyKey = request.IdempotencyKey }, cancellationToken);
            return new PaymentIntentResult(true, intent.Id, intent.ClientSecret);
        }
        catch (StripeException ex)
        {
            return new PaymentIntentResult(false, Error: ex.Message);
        }
    }

    public async Task<RefundResult> RefundAsync(string gatewayPaymentId, decimal amount, string reason, CancellationToken cancellationToken = default)
    {
        var service = new RefundService();
        var options = new RefundCreateOptions
        {
            PaymentIntent = gatewayPaymentId,
            Amount = (long)(amount * 100),
            Reason = reason.Contains("fraud", StringComparison.OrdinalIgnoreCase) ? "fraudulent" : "requested_by_customer"
        };

        try
        {
            var refund = await service.CreateAsync(options, cancellationToken: cancellationToken);
            return new RefundResult(true, refund.Id);
        }
        catch (StripeException ex)
        {
            return new RefundResult(false, Error: ex.Message);
        }
    }

    public Task<WebhookEventResult?> ParseWebhookAsync(string body, string signatureHeader, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_webhookSecret))
            return Task.FromResult<WebhookEventResult?>(null);

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(body, signatureHeader, _webhookSecret);
        }
        catch
        {
            return Task.FromResult<WebhookEventResult?>(null);
        }

        var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
        var orderId = paymentIntent != null
            ? paymentIntent.Metadata.TryGetValue("orderId", out var o) ? int.Parse(o) : (int?)null
            : null;

        return Task.FromResult<WebhookEventResult?>(new WebhookEventResult(
            stripeEvent.Id,
            stripeEvent.Type,
            orderId,
            Succeeded: stripeEvent.Type == "payment_intent.succeeded",
            Refunded: stripeEvent.Type == "charge.refunded" || stripeEvent.Type == "charge.refunded",
            RefundAmount: null,
            GatewayPaymentId: paymentIntent?.Id));
    }
}
