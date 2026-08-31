using EcomApi.Data;
using EcomApi.Models;
using EcomApi.Repositories;
using EcomApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EcomApi.Tests;

public class AbandonedCartRecoveryTests
{
    private static ApplicationDbContext CreateContext(string name)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new ApplicationDbContext(options);
    }

    private static AbandonedCartScanRunner CreateRunner(
        ApplicationDbContext context,
        Mock<ICouponRepository>? coupon = null,
        Mock<INotificationQueue>? queue = null,
        ISettingsProvider? settings = null)
    {
        return new AbandonedCartScanRunner(
            context,
            coupon?.Object ?? new Mock<ICouponRepository>().Object,
            queue?.Object ?? new Mock<INotificationQueue>().Object,
            settings ?? new Mock<ISettingsProvider>().Object,
            Mock.Of<ILogger<AbandonedCartScanRunner>>());
    }

    private static ISettingsProvider BuildSettings(int hours = 24, int resendDays = 7,
        bool couponEnabled = false, decimal couponAmount = 100m, decimal minTotal = 0m)
    {
        var s = new Mock<ISettingsProvider>();
        s.Setup(x => x.GetAsync("Cart:AbandonmentHours", 24, It.IsAny<CancellationToken>())).ReturnsAsync(hours);
        s.Setup(x => x.GetAsync("Cart:ResendDays", 7, It.IsAny<CancellationToken>())).ReturnsAsync(resendDays);
        s.Setup(x => x.GetAsync("Cart:RecoveryCouponEnabled", false, It.IsAny<CancellationToken>())).ReturnsAsync(couponEnabled);
        s.Setup(x => x.GetAsync("Cart:RecoveryCouponAmount", 100m, It.IsAny<CancellationToken>())).ReturnsAsync(couponAmount);
        s.Setup(x => x.GetAsync("Cart:MinCartTotal", 100m, It.IsAny<CancellationToken>())).ReturnsAsync(minTotal);
        s.Setup(x => x.GetBaseUrl()).Returns("http://localhost:4200");
        return s.Object;
    }

    private static (User user, Cart cart) SeedCart(
        ApplicationDbContext context,
        DateTime updatedAt,
        decimal unitPrice = 200m,
        int quantity = 2,
        bool optOut = false,
        DateTime? lastNotified = null,
        bool isActive = true)
    {
        var user = new User
        {
            Id = 1,
            Email = "shopper@example.com",
            Username = "shopper",
            Role = "Customer",
            IsActive = isActive,
            AbandonedCartOptOut = optOut,
            LastAbandonedCartNotifiedAt = lastNotified
        };
        var product = new Product { Id = 1, Name = "Widget", Price = unitPrice, Stock = 5 };
        var cart = new Cart { Id = 10, UserId = user.Id, User = user, UpdatedAt = updatedAt };
        cart.Items.Add(new CartItem { Id = 100, Cart = cart, CartId = cart.Id, Product = product, ProductId = product.Id, Quantity = quantity, UnitPrice = unitPrice, TotalPrice = unitPrice * quantity });
        context.Users.Add(user);
        context.Products.Add(product);
        context.Carts.Add(cart);
        context.SaveChanges();
        return (user, cart);
    }

    [Fact]
    public async Task RunAsync_EnqueuesEmail_WhenCartIsOldEnough()
    {
        using var context = CreateContext(nameof(RunAsync_EnqueuesEmail_WhenCartIsOldEnough));
        SeedCart(context, DateTime.UtcNow.AddHours(-48), unitPrice: 500m);

        var queue = new Mock<INotificationQueue>();
        var runner = CreateRunner(context, queue: queue, settings: BuildSettings(hours: 24, minTotal: 0m));

        var sent = await runner.RunAsync();

        Assert.Equal(1, sent);
        queue.Verify(q => q.EnqueueAsync(It.Is<NotificationMessage>(m =>
            m.Type == NotificationType.AbandonedCart
            && m.Email == "shopper@example.com"
            && m.Subject == "You left items in your cart"
            && m.HtmlBody!.Contains("left something behind")
            && m.HtmlBody!.Contains("Unsubscribe from abandoned-cart reminders")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunAsync_SkipsRecentCarts()
    {
        using var context = CreateContext(nameof(RunAsync_SkipsRecentCarts));
        SeedCart(context, DateTime.UtcNow.AddHours(-2), unitPrice: 500m);

        var queue = new Mock<INotificationQueue>();
        var runner = CreateRunner(context, queue: queue, settings: BuildSettings(hours: 24, minTotal: 0m));

        var sent = await runner.RunAsync();

        Assert.Equal(0, sent);
        queue.Verify(q => q.EnqueueAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_SkipsOptedOutUsers()
    {
        using var context = CreateContext(nameof(RunAsync_SkipsOptedOutUsers));
        SeedCart(context, DateTime.UtcNow.AddHours(-48), unitPrice: 500m, optOut: true);

        var queue = new Mock<INotificationQueue>();
        var runner = CreateRunner(context, queue: queue, settings: BuildSettings(hours: 24, minTotal: 0m));

        var sent = await runner.RunAsync();

        Assert.Equal(0, sent);
        queue.Verify(q => q.EnqueueAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_SkipsLockedOutOrInactiveUsers()
    {
        using var context = CreateContext(nameof(RunAsync_SkipsLockedOutOrInactiveUsers));
        SeedCart(context, DateTime.UtcNow.AddHours(-48), unitPrice: 500m, isActive: false);

        var queue = new Mock<INotificationQueue>();
        var runner = CreateRunner(context, queue: queue, settings: BuildSettings(hours: 24, minTotal: 0m));

        var sent = await runner.RunAsync();

        Assert.Equal(0, sent);
    }

    [Fact]
    public async Task RunAsync_RespectsCooldown()
    {
        using var context = CreateContext(nameof(RunAsync_RespectsCooldown));
        SeedCart(context, DateTime.UtcNow.AddHours(-48), unitPrice: 500m,
            lastNotified: DateTime.UtcNow.AddDays(-2));

        var queue = new Mock<INotificationQueue>();
        var runner = CreateRunner(context, queue: queue, settings: BuildSettings(hours: 24, resendDays: 7, minTotal: 0m));

        var sent = await runner.RunAsync();

        Assert.Equal(0, sent);
    }

    [Fact]
    public async Task RunAsync_SendsAfterCooldownWindow()
    {
        using var context = CreateContext(nameof(RunAsync_SendsAfterCooldownWindow));
        SeedCart(context, DateTime.UtcNow.AddHours(-48), unitPrice: 500m,
            lastNotified: DateTime.UtcNow.AddDays(-10));

        var queue = new Mock<INotificationQueue>();
        var runner = CreateRunner(context, queue: queue, settings: BuildSettings(hours: 24, resendDays: 7, minTotal: 0m));

        var sent = await runner.RunAsync();

        Assert.Equal(1, sent);
    }

    [Fact]
    public async Task RunAsync_SkipsCartsBelowMinTotal()
    {
        using var context = CreateContext(nameof(RunAsync_SkipsCartsBelowMinTotal));
        SeedCart(context, DateTime.UtcNow.AddHours(-48), unitPrice: 10m, quantity: 1);

        var queue = new Mock<INotificationQueue>();
        var runner = CreateRunner(context, queue: queue, settings: BuildSettings(hours: 24, minTotal: 100m));

        var sent = await runner.RunAsync();

        Assert.Equal(0, sent);
    }

    [Fact]
    public async Task RunAsync_CreatesCoupon_WhenEnabled()
    {
        using var context = CreateContext(nameof(RunAsync_CreatesCoupon_WhenEnabled));
        SeedCart(context, DateTime.UtcNow.AddHours(-48), unitPrice: 500m);

        var queue = new Mock<INotificationQueue>();
        var coupon = new Mock<ICouponRepository>();
        coupon.Setup(c => c.AddAsync(It.IsAny<Coupon>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Coupon c, CancellationToken _) => c);
        var runner = CreateRunner(context, coupon: coupon, queue: queue,
            settings: BuildSettings(hours: 24, couponEnabled: true, couponAmount: 150m, minTotal: 0m));

        var sent = await runner.RunAsync();

        Assert.Equal(1, sent);
        coupon.Verify(c => c.AddAsync(It.Is<Coupon>(cp =>
            cp.Type == CouponType.FixedAmount
            && cp.Value == 150m
            && cp.MaxUses == 1
            && cp.IsActive
            && cp.ExpiresAt > DateTime.UtcNow
            && cp.Code.StartsWith("COMEBACK-1-")),
            It.IsAny<CancellationToken>()), Times.Once);
        queue.Verify(q => q.EnqueueAsync(It.Is<NotificationMessage>(m =>
            m.HtmlBody!.Contains("COMEBACK-1-")
            && m.HtmlBody!.Contains("150")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunAsync_DoesNotCreateCoupon_WhenDisabled()
    {
        using var context = CreateContext(nameof(RunAsync_DoesNotCreateCoupon_WhenDisabled));
        SeedCart(context, DateTime.UtcNow.AddHours(-48), unitPrice: 500m);

        var queue = new Mock<INotificationQueue>();
        var coupon = new Mock<ICouponRepository>();
        var runner = CreateRunner(context, coupon: coupon, queue: queue,
            settings: BuildSettings(hours: 24, couponEnabled: false, minTotal: 0m));

        await runner.RunAsync();

        coupon.Verify(c => c.AddAsync(It.IsAny<Coupon>(), It.IsAny<CancellationToken>()), Times.Never);
        queue.Verify(q => q.EnqueueAsync(It.Is<NotificationMessage>(m => !m.HtmlBody!.Contains("COMEBACK-")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunAsync_SendsEmailEvenIfCouponCreationFails()
    {
        using var context = CreateContext(nameof(RunAsync_SendsEmailEvenIfCouponCreationFails));
        SeedCart(context, DateTime.UtcNow.AddHours(-48), unitPrice: 500m);

        var queue = new Mock<INotificationQueue>();
        var coupon = new Mock<ICouponRepository>();
        coupon.Setup(c => c.AddAsync(It.IsAny<Coupon>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        var runner = CreateRunner(context, coupon: coupon, queue: queue,
            settings: BuildSettings(hours: 24, couponEnabled: true, minTotal: 0m));

        var sent = await runner.RunAsync();

        Assert.Equal(1, sent);
        queue.Verify(q => q.EnqueueAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunAsync_UpdatesLastNotifiedTimestamp()
    {
        using var context = CreateContext(nameof(RunAsync_UpdatesLastNotifiedTimestamp));
        var (user, _) = SeedCart(context, DateTime.UtcNow.AddHours(-48), unitPrice: 500m);

        var queue = new Mock<INotificationQueue>();
        var runner = CreateRunner(context, queue: queue, settings: BuildSettings(hours: 24, minTotal: 0m));

        Assert.Null(user.LastAbandonedCartNotifiedAt);
        await runner.RunAsync();

        var reloaded = await context.Users.FindAsync(user.Id);
        Assert.NotNull(reloaded!.LastAbandonedCartNotifiedAt);
        Assert.True(reloaded.LastAbandonedCartNotifiedAt > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task RunAsync_ReturnsZero_WhenNoCandidates()
    {
        using var context = CreateContext(nameof(RunAsync_ReturnsZero_WhenNoCandidates));
        var queue = new Mock<INotificationQueue>();
        var runner = CreateRunner(context, queue: queue, settings: BuildSettings(hours: 24, minTotal: 0m));

        var sent = await runner.RunAsync();

        Assert.Equal(0, sent);
        queue.Verify(q => q.EnqueueAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void EmailTemplates_AbandonedCart_IncludesUnsubscribeLink()
    {
        var html = EmailTemplates.AbandonedCart(
            "Jane",
            "http://localhost:4200/cart",
            "COMEBACK-1-A",
            100m,
            DateTime.UtcNow.AddDays(7),
            "http://localhost:4200/api/auth/abandoned-cart/unsubscribe?token=abc");

        Assert.Contains("Jane", html);
        Assert.Contains("COMEBACK-1-A", html);
        Assert.Contains("Unsubscribe from abandoned-cart reminders", html);
        Assert.Contains("token=abc", html);
    }

    [Fact]
    public void EmailTemplates_AbandonedCart_OmitsCouponSection_WhenNoCode()
    {
        var html = EmailTemplates.AbandonedCart(
            "Jane",
            "http://localhost:4200/cart",
            null,
            0m,
            null,
            "http://localhost:4200/api/auth/abandoned-cart/unsubscribe?token=abc");

        Assert.DoesNotContain("As a welcome back", html);
        Assert.Contains("Unsubscribe from abandoned-cart reminders", html);
    }
}