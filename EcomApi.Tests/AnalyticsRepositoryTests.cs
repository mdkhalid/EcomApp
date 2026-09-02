using EcomApi.Data;
using EcomApi.Models;
using EcomApi.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EcomApi.Tests;

public class AnalyticsRepositoryTests
{
    private static ApplicationDbContext CreateContext(string name)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new ApplicationDbContext(options);
    }

    private static Order BuildOrder(int id, decimal total, decimal discount, string? couponCode, OrderStatus status, DateTime createdAt)
        => new()
        {
            Id = id,
            Status = status,
            CouponCode = couponCode,
            TotalAmount = total,
            DiscountAmount = discount,
            ShippingName = "X",
            ShippingAddress = "A",
            ShippingCity = "C",
            ShippingZip = "1",
            CreatedAt = createdAt
        };

    [Fact]
    public async Task GetCouponPerformanceAsync_AggregatesPerCoupon_AndSplitsCouponVsNonCouponOrders()
    {
        using var context = CreateContext("coupon-analytics");
        var now = DateTime.UtcNow;

        var save10 = new Coupon { Id = 1, Code = "SAVE10", Type = CouponType.Percentage, Value = 10m, ExpiresAt = now.AddDays(30) };
        var welcome = new Coupon { Id = 2, Code = "WELCOME", Type = CouponType.FixedAmount, Value = 50m, ExpiresAt = now.AddDays(30) };
        context.Coupons.AddRange(save10, welcome);

        var order1 = BuildOrder(1, 400m, 100m, "SAVE10", OrderStatus.Delivered, now);
        var order2 = BuildOrder(2, 600m, 150m, "SAVE10", OrderStatus.Delivered, now);
        var order3 = BuildOrder(3, 450m, 50m, "WELCOME", OrderStatus.Delivered, now);
        var orderCancelled = BuildOrder(4, 999m, 60m, "SAVE10", OrderStatus.Cancelled, now);
        var orderNoCoupon = BuildOrder(5, 700m, 0m, null, OrderStatus.Delivered, now);
        context.Orders.AddRange(order1, order2, order3, orderCancelled, orderNoCoupon);

        context.CouponUsages.AddRange(
            new CouponUsage { CouponId = 1, Coupon = save10, OrderId = 1, Order = order1, UserId = 11, DiscountAmount = 100m, UsedAt = now },
            new CouponUsage { CouponId = 1, Coupon = save10, OrderId = 2, Order = order2, UserId = 11, DiscountAmount = 150m, UsedAt = now },
            new CouponUsage { CouponId = 2, Coupon = welcome, OrderId = 3, Order = order3, UserId = 5, DiscountAmount = 50m, UsedAt = now },
            new CouponUsage { CouponId = 1, Coupon = save10, OrderId = 4, Order = orderCancelled, UserId = 5, DiscountAmount = 60m, UsedAt = now });
        await context.SaveChangesAsync();

        var repo = new AnalyticsRepository(context);
        var report = await repo.GetCouponPerformanceAsync(now.AddDays(-1), now.AddDays(1));

        Assert.Equal(3, report.OrdersWithCoupon);
        Assert.Equal(1450m, report.RevenueWithCoupon);
        Assert.Equal(300m, report.TotalDiscount);
        Assert.Equal(1, report.OrdersWithoutCoupon);
        Assert.Equal(700m, report.RevenueWithoutCoupon);

        Assert.Equal(2, report.Coupons.Count);

        var save10Perf = Assert.Single(report.Coupons, c => c.Code == "SAVE10");
        Assert.Equal(2, save10Perf.Redemptions);
        Assert.Equal(1, save10Perf.UniqueCustomers);
        Assert.Equal(250m, save10Perf.DiscountedTotal);
        Assert.Equal(1000m, save10Perf.Revenue);

        var welcomePerf = Assert.Single(report.Coupons, c => c.Code == "WELCOME");
        Assert.Equal(1, welcomePerf.Redemptions);
        Assert.Equal(1, welcomePerf.UniqueCustomers);
        Assert.Equal(50m, welcomePerf.DiscountedTotal);
        Assert.Equal(450m, welcomePerf.Revenue);
    }

    [Fact]
    public async Task GetCouponPerformanceAsync_RespectsDateRange_AndGuestUsers()
    {
        using var context = CreateContext("coupon-analytics-range");
        var now = DateTime.UtcNow;

        var coupon = new Coupon { Id = 1, Code = "SAVE10", Type = CouponType.Percentage, Value = 10m, ExpiresAt = now.AddDays(30) };
        context.Coupons.Add(coupon);

        var inRange = BuildOrder(1, 500m, 100m, "SAVE10", OrderStatus.Delivered, now.AddDays(-35));
        var outOfRange = BuildOrder(2, 200m, 40m, "SAVE10", OrderStatus.Delivered, now);
        context.Orders.AddRange(inRange, outOfRange);

        context.CouponUsages.AddRange(
            new CouponUsage { CouponId = 1, Coupon = coupon, OrderId = 1, Order = inRange, UserId = 0, DiscountAmount = 100m, UsedAt = now.AddDays(-35) },
            new CouponUsage { CouponId = 1, Coupon = coupon, OrderId = 2, Order = outOfRange, UserId = 7, DiscountAmount = 40m, UsedAt = now });
        await context.SaveChangesAsync();

        var repo = new AnalyticsRepository(context);
        var report = await repo.GetCouponPerformanceAsync(now.AddDays(-40), now.AddDays(-30));

        var perf = Assert.Single(report.Coupons);
        Assert.Equal(1, perf.Redemptions);
        Assert.Equal(0, perf.UniqueCustomers);
        Assert.Equal(100m, perf.DiscountedTotal);
        Assert.Equal(500m, perf.Revenue);
        Assert.Equal(1, report.OrdersWithCoupon);
        Assert.Equal(0, report.OrdersWithoutCoupon);
    }
}
