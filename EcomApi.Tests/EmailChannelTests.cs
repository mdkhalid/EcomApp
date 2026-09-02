using EcomApi.Models;
using EcomApi.Repositories;
using EcomApi.Services;
using EcomApi.Services.NotificationChannels;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EcomApi.Tests;

public class EmailChannelTests
{
    private readonly Mock<IEmailService> _email = new();
    private readonly Mock<IOrderRepository> _orders = new();
    private readonly Mock<IInvoiceService> _invoice = new();

    private EmailChannel CreateChannel()
        => new(_email.Object, _orders.Object, _invoice.Object, Mock.Of<ILogger<EmailChannel>>());

    private static NotificationMessage Confirmation(int orderId) => new()
    {
        Type = NotificationType.OrderConfirmation,
        Email = "customer@example.com",
        Subject = "Order Confirmed",
        HtmlBody = "<p>thanks</p>",
        OrderId = orderId
    };

    [Fact]
    public async Task SendAsync_OrderConfirmation_AttachesInvoicePdf()
    {
        var order = new Order { Id = 42 };
        _orders.Setup(r => r.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _invoice.Setup(i => i.GenerateInvoicePdfAsync(order)).ReturnsAsync(new byte[] { 1, 2, 3, 4 });

        IReadOnlyList<EmailAttachment>? captured = null;
        _email.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<IReadOnlyList<EmailAttachment>?>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, IReadOnlyList<EmailAttachment>?, CancellationToken>(
                (_, _, _, attachments, _) => captured = attachments);

        await CreateChannel().SendAsync(Confirmation(42));

        Assert.NotNull(captured);
        var attachment = Assert.Single(captured!);
        Assert.Equal("invoice-42.pdf", attachment.FileName);
        Assert.Equal("application/pdf", attachment.ContentType);
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, attachment.Data);
    }

    [Fact]
    public async Task SendAsync_NonConfirmation_DoesNotLoadOrderOrAttach()
    {
        var message = new NotificationMessage
        {
            Type = NotificationType.Welcome,
            Email = "customer@example.com",
            Subject = "Welcome",
            HtmlBody = "<p>hi</p>"
        };

        IReadOnlyList<EmailAttachment>? captured = null;
        _email.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<IReadOnlyList<EmailAttachment>?>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, IReadOnlyList<EmailAttachment>?, CancellationToken>(
                (_, _, _, attachments, _) => captured = attachments);

        await CreateChannel().SendAsync(message);

        _orders.Verify(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Null(captured);
    }

    [Fact]
    public async Task SendAsync_OrderMissing_SendsWithoutAttachment()
    {
        _orders.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Order?)null);

        IReadOnlyList<EmailAttachment>? captured = null;
        _email.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<IReadOnlyList<EmailAttachment>?>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, IReadOnlyList<EmailAttachment>?, CancellationToken>(
                (_, _, _, attachments, _) => captured = attachments);

        await CreateChannel().SendAsync(Confirmation(99));

        _email.Verify(e => e.SendAsync("customer@example.com", It.IsAny<string>(), It.IsAny<string>(),
            null, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Null(captured);
    }

    [Fact]
    public async Task SendAsync_InvoiceGenerationFails_SendsWithoutAttachment()
    {
        var order = new Order { Id = 7 };
        _orders.Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _invoice.Setup(i => i.GenerateInvoicePdfAsync(order)).ThrowsAsync(new InvalidOperationException("boom"));

        IReadOnlyList<EmailAttachment>? captured = null;
        _email.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<IReadOnlyList<EmailAttachment>?>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, IReadOnlyList<EmailAttachment>?, CancellationToken>(
                (_, _, _, attachments, _) => captured = attachments);

        await CreateChannel().SendAsync(Confirmation(7));

        _email.Verify(e => e.SendAsync("customer@example.com", It.IsAny<string>(), It.IsAny<string>(),
            null, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Null(captured);
    }
}
