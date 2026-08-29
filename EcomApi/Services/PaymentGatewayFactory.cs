using EcomApi.Models;
using Stripe;
using Microsoft.Extensions.Configuration;

namespace EcomApi.Services;

/// <summary>Selects the active payment gateway. Uses Stripe when a secret key is
/// configured, otherwise the Mock gateway so the flow runs without credentials.</summary>
public class PaymentGatewayFactory
{
    private readonly StripeGateway _stripe;
    private readonly MockGateway _mock = new();

    public PaymentGatewayFactory(StripeGateway stripe)
    {
        _stripe = stripe;
    }

    public IPaymentGateway GetGateway()
    {
        return string.IsNullOrWhiteSpace(StripeConfiguration.ApiKey)
            ? _mock
            : _stripe;
    }
}
