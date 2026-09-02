using EcomApi.Data;
using EcomApi.Models;
using EcomApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EcomApi.Tests;

public class DailyDigestRunnerTests
{
    private static ApplicationDbContext CreateContext(string name)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task RunAsync_Disabled_ReturnsFalseAndDoesNotEnqueue()
    {
        using var context = CreateContext("digest-disabled");
        var queue = new Mock<INotificationQueue>();
        var settings = new Mock<ISettingsProvider>();
        settings.Setup(s => s.GetAsync("Digest:Enabled", false, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        settings.Setup(s => s.GetRawAsync("Digest:Recipient", It.IsAny<CancellationToken>())).ReturnsAsync("admin@example.com");

        var runner = new DailyDigestRunner(context, queue.Object, settings.Object, Mock.Of<ILogger<DailyDigestRunner>>());

        var result = await runner.RunAsync();

        Assert.False(result);
        queue.Verify(q => q.EnqueueAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_NoRecipient_ReturnsFalseAndLogsWarning()
    {
        using var context = CreateContext("digest-norecipient");
        var queue = new Mock<INotificationQueue>();
        var settings = new Mock<ISettingsProvider>();
        settings.Setup(s => s.GetAsync("Digest:Enabled", false, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        settings.Setup(s => s.GetRawAsync("Digest:Recipient", It.IsAny<CancellationToken>())).ReturnsAsync("");

        var runner = new DailyDigestRunner(context, queue.Object, settings.Object, Mock.Of<ILogger<DailyDigestRunner>>());

        var result = await runner.RunAsync();

        Assert.False(result);
        queue.Verify(q => q.EnqueueAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_EnabledWithData_EnqueuesDigestEmail()
    {
        using var context = CreateContext("digest-withdata");
        var now = DateTime.UtcNow;
        var yesterday = now.Date.AddDays(-1);

        context.Orders.AddRange(
            new Order
            {
                Id = 1,
                Status = OrderStatus.Delivered,
                TotalAmount = 500m,
                CreatedAt = yesterday.AddHours(10),
                ShippingName = "A",
                ShippingAddress = "B",
                ShippingCity = "C",
                ShippingZip = "1"
            },
            new Order
            {
                Id = 2,
                Status = OrderStatus.Shipped,
                TotalAmount = 300m,
                CreatedAt = yesterday.AddHours(14),
                ShippingName = "A",
                ShippingAddress = "B",
                ShippingCity = "C",
                ShippingZip = "1"
            },
            new Order
            {
                Id = 3,
                Status = OrderStatus.Cancelled,
                TotalAmount = 100m,
                CreatedAt = yesterday.AddHours(16),
                ShippingName = "A",
                ShippingAddress = "B",
                ShippingCity = "C",
                ShippingZip = "1"
            });
        context.Users.AddRange(
            new User { Id = 1, Email = "u1@test.com", Username = "u1", PasswordHash = "h", CreatedAt = yesterday.AddHours(12) },
            new User { Id = 2, Email = "u2@test.com", Username = "u2", PasswordHash = "h", CreatedAt = yesterday.AddHours(18) });
        context.ReturnRequests.AddRange(
            new ReturnRequest { Id = 1, OrderId = 1, UserId = 1, Status = ReturnStatus.Requested },
            new ReturnRequest { Id = 2, OrderId = 2, UserId = 1, Status = ReturnStatus.Approved },
            new ReturnRequest { Id = 3, OrderId = 3, UserId = 2, Status = ReturnStatus.Rejected });
        context.Products.AddRange(
            new Product { Id = 1, Name = "Low Stock Item", Stock = 5, IsActive = true },
            new Product { Id = 2, Name = "Normal Stock", Stock = 50, IsActive = true });
        await context.SaveChangesAsync();

        var queue = new Mock<INotificationQueue>();
        var settings = new Mock<ISettingsProvider>();
        settings.Setup(s => s.GetAsync("Digest:Enabled", false, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        settings.Setup(s => s.GetRawAsync("Digest:Recipient", It.IsAny<CancellationToken>())).ReturnsAsync("admin@example.com");
        settings.Setup(s => s.GetAsync("Digest:LowStockThreshold", 10, It.IsAny<CancellationToken>())).ReturnsAsync(10);

        NotificationMessage? captured = null;
        queue.Setup(q => q.EnqueueAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()))
            .Callback<NotificationMessage, CancellationToken>((m, _) => captured = m)
            .Returns(Task.CompletedTask);

        var runner = new DailyDigestRunner(context, queue.Object, settings.Object, Mock.Of<ILogger<DailyDigestRunner>>());

        var result = await runner.RunAsync();

Assert.True(result);
        Assert.NotNull(captured);
        Assert.Equal("admin@example.com", captured!.Email);
        Assert.Contains("Daily Digest", captured.Subject);
        Assert.Contains("Orders (yesterday)", captured.HtmlBody);
        Assert.Contains("800.00", captured.HtmlBody);
        Assert.Contains("New Users (yesterday)", captured.HtmlBody);
        Assert.Contains("Pending Returns", captured.HtmlBody);
        Assert.Contains("Low Stock Item", captured.HtmlBody);
        Assert.DoesNotContain("Normal Stock", captured.HtmlBody);
    }
}