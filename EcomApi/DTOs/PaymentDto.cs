using System.ComponentModel.DataAnnotations;

namespace EcomApi.DTOs;

public class CreatePaymentIntentDto
{
    [Required]
    public int OrderId { get; set; }
}

public class MockConfirmDto
{
    [Required]
    public int OrderId { get; set; }
}

public class RefundOrderDto
{
    [Required]
    public int OrderId { get; set; }
    public decimal? Amount { get; set; }
}

public class PaymentConfigDto
{
    public string Gateway { get; set; } = string.Empty;
    public string? PublishableKey { get; set; }
}
