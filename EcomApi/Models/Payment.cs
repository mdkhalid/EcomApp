namespace EcomApi.Models;

public enum PaymentGateway
{
    Stripe,
    Mock
}

public enum PaymentStatus
{
    Pending,
    Succeeded,
    Failed,
    Refunded,
    PartiallyRefunded
}

public class Payment
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public PaymentGateway Gateway { get; set; }
    public string GatewayPaymentId { get; set; } = string.Empty;
    public string? ClientSecret { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string? IdempotencyKey { get; set; }
    public string? RawEventJson { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Records a processed gateway webhook event so the same event is never applied twice.</summary>
public class ProcessedWebhookEvent
{
    public int Id { get; set; }
    public string EventId { get; set; } = string.Empty;
    public string Gateway { get; set; } = string.Empty;
    public string? RawJson { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}
