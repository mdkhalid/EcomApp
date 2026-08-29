using EcomApi.Data;
using EcomApi.Models;
using EcomApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace EcomApi.Tests;

public class PaymentServiceTests
{
    private static ApplicationDbContext CreateContext(string name)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task PricingService_AppliesTaxOnDiscountedSubtotal_AndFreeShippingOverThreshold()
    {
        using var context = CreateContext("pricing");
        context.TaxRates.Add(new TaxRate { Name = "GST", Percentage = 18m, IsDefault = true });
        context.ShippingZones.Add(new ShippingZone
        {
            Name = "All India",
            Regions = "ALL",
            Rates = new List<ShippingRate>
            {
                new ShippingRate { Method = "Standard", Rate = 50m, FreeOverAmount = 500m, MinOrderAmount = 0, IsActive = true }
            }
        });
        await context.SaveChangesAsync();

        var service = new PricingService(context);

        // 600 subtotal, 100 discount -> 500 discounted -> free shipping, tax 18% of 500 = 90
        var breakdown = await service.ComputeTotalsAsync(600m, 100m, "Pune", "411001");

        Assert.Equal(600m, breakdown.Subtotal);
        Assert.Equal(100m, breakdown.Discount);
        Assert.Equal(0m, breakdown.Shipping);
        Assert.Equal(90m, breakdown.Tax);
        Assert.Equal(590m, breakdown.Total);
    }

    [Fact]
    public async Task PricingService_ChargesShipping_WhenBelowFreeThreshold()
    {
        using var context = CreateContext("pricing2");
        context.TaxRates.Add(new TaxRate { Name = "GST", Percentage = 10m, IsDefault = true });
        context.ShippingZones.Add(new ShippingZone
        {
            Name = "All India",
            Regions = "ALL",
            Rates = new List<ShippingRate>
            {
                new ShippingRate { Method = "Standard", Rate = 40m, FreeOverAmount = 500m, MinOrderAmount = 0, IsActive = true }
            }
        });
        await context.SaveChangesAsync();

        var service = new PricingService(context);

        var breakdown = await service.ComputeTotalsAsync(300m, 0m, "Pune", "411001");

        Assert.Equal(300m, breakdown.Subtotal);
        Assert.Equal(40m, breakdown.Shipping);
        Assert.Equal(30m, breakdown.Tax);
        Assert.Equal(370m, breakdown.Total);
    }

    [Fact]
    public void PaymentGatewayFactory_ReturnsMock_WhenNoStripeKey()
    {
        var config = new ConfigurationBuilder().Build();
        var factory = new PaymentGatewayFactory(new StripeGateway(config));

        var gateway = factory.GetGateway();

        Assert.Equal(PaymentGateway.Mock, gateway.Gateway);
    }

    [Fact]
    public async Task MockGateway_CreatesIntentAndRefunds()
    {
        var config = new ConfigurationBuilder().Build();
        var factory = new PaymentGatewayFactory(new StripeGateway(config));
        var gateway = factory.GetGateway();

        var intent = await gateway.CreatePaymentIntentAsync(new CreatePaymentRequest(1, 100m, "inr", "k", "a@b.com"));
        Assert.True(intent.Success);
        Assert.StartsWith("mock_pi_", intent.GatewayPaymentId);

        var refund = await gateway.RefundAsync(intent.GatewayPaymentId!, 100m, "requested_by_customer");
        Assert.True(refund.Success);
    }

    [Fact]
    public async Task WebhookProcessor_MarksOrderPaid_AndSendsConfirmation()
    {
        using var context = CreateContext("webhook");
        var order = new Order
        {
            Id = 1,
            UserId = 1,
            Status = OrderStatus.AwaitingPayment,
            PaymentStatus = PaymentStatus.Pending,
            TotalAmount = 100m,
            ShippingName = "X",
            ShippingAddress = "A",
            ShippingCity = "C",
            ShippingZip = "1",
            CustomerEmail = "a@b.com"
        };
        order.Payments.Add(new Payment
        {
            OrderId = 1,
            Gateway = PaymentGateway.Mock,
            GatewayPaymentId = "mock_pi_1",
            Amount = 100m,
            Currency = "INR",
            Status = PaymentStatus.Pending,
            IdempotencyKey = "order-1"
        });
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var notification = new Mock<INotificationService>();
        var processor = new PaymentWebhookProcessor(context, notification.Object, new Mock<Microsoft.Extensions.Logging.ILogger<PaymentWebhookProcessor>>().Object);

        await processor.ProcessSucceededAsync(1, "mock_pi_1");

        var reloaded = await context.Orders.Include(o => o.Payments).FirstAsync(o => o.Id == 1);
        Assert.Equal(OrderStatus.Paid, reloaded.Status);
        Assert.Equal(PaymentStatus.Succeeded, reloaded.PaymentStatus);
        Assert.Equal(PaymentStatus.Succeeded, reloaded.Payments[0].Status);
        notification.Verify(n => n.SendOrderConfirmationAsync(It.IsAny<Order>()), Times.Once);
    }

    [Fact]
    public async Task WebhookProcessor_IsIdempotent()
    {
        using var context = CreateContext("webhook-idem");
        var order = new Order
        {
            Id = 2,
            UserId = 1,
            Status = OrderStatus.AwaitingPayment,
            PaymentStatus = PaymentStatus.Pending,
            TotalAmount = 50m,
            ShippingName = "X",
            ShippingAddress = "A",
            ShippingCity = "C",
            ShippingZip = "1",
            CustomerEmail = "a@b.com"
        };
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var notification = new Mock<INotificationService>();
        var processor = new PaymentWebhookProcessor(context, notification.Object, new Mock<Microsoft.Extensions.Logging.ILogger<PaymentWebhookProcessor>>().Object);

        await processor.ProcessSucceededAsync(2);
        await processor.ProcessSucceededAsync(2);

        notification.Verify(n => n.SendOrderConfirmationAsync(It.IsAny<Order>()), Times.Once);
        Assert.Equal(OrderStatus.Paid, (await context.Orders.FindAsync(2))!.Status);
    }
}
