using EcomApi.Data;
using EcomApi.DTOs;
using EcomApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EcomApi.Repositories;

public class AnalyticsRepository : IAnalyticsRepository
{
    private readonly ApplicationDbContext _context;

    public AnalyticsRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    private static readonly HashSet<OrderStatus> RevenueStatuses = new()
    {
        OrderStatus.Processing,
        OrderStatus.Shipped,
        OrderStatus.OutForDelivery,
        OrderStatus.Delivered
    };

    public async Task<RevenueSummaryDto> GetRevenueAsync(string period)
    {
        period = (period ?? "monthly").ToLowerInvariant();
        if (period is not ("daily" or "weekly" or "monthly"))
            period = "monthly";

        var now = DateTime.UtcNow;
        DateTime start;
        int bucketCount;
        Func<DateTime, (DateTime bucketStart, string label)> toBucket;

        switch (period)
        {
            case "daily":
                start = now.Date.AddDays(-13);
                bucketCount = 14;
                toBucket = d => (d.Date, d.ToString("dd MMM"));
                break;
            case "weekly":
                start = now.Date.AddDays(-7 * 11);
                bucketCount = 12;
                toBucket = d =>
                {
                    var diff = (7 + (int)d.DayOfWeek - (int)DayOfWeek.Monday) % 7;
                    var weekStart = d.Date.AddDays(-diff);
                    return (weekStart, $"W{System.Globalization.ISOWeek.GetWeekOfYear(weekStart)}");
                };
                break;
            default:
                start = new DateTime(now.Year, now.Month, 1).AddMonths(-11);
                bucketCount = 12;
                toBucket = d => (new DateTime(d.Year, d.Month, 1), d.ToString("MMM yyyy"));
                break;
        }

        var orders = await _context.Orders
            .AsNoTracking()
            .Where(o => o.CreatedAt >= start && o.Status != OrderStatus.Cancelled && o.Status != OrderStatus.Returned)
            .Select(o => new { o.CreatedAt, o.TotalAmount })
            .ToListAsync();

        var buckets = Enumerable.Range(0, bucketCount)
            .Select(i =>
            {
                var (bucketStart, _) = toBucket(start.AddDays(i));
                return new
                {
                    bucketStart,
                    label = toBucket(bucketStart).label,
                    revenue = 0m,
                    count = 0
                };
            })
            .ToList();

        foreach (var o in orders)
        {
            var (bs, _) = toBucket(o.CreatedAt);
            if (bs < start) continue;
            var idx = buckets.FindIndex(b => b.bucketStart == bs);
            if (idx < 0) continue;
            buckets[idx] = new
            {
                buckets[idx].bucketStart,
                buckets[idx].label,
                revenue = buckets[idx].revenue + o.TotalAmount,
                count = buckets[idx].count + 1
            };
        }

        var points = buckets.Select(b => new RevenuePointDto
        {
            Date = b.bucketStart,
            Label = b.label,
            Revenue = b.revenue,
            OrderCount = b.count
        }).ToList();

        return new RevenueSummaryDto
        {
            Period = period,
            TotalRevenue = points.Sum(p => p.Revenue),
            TotalOrders = points.Sum(p => p.OrderCount),
            AverageOrderValue = points.Sum(p => p.OrderCount) > 0
                ? Math.Round(points.Sum(p => p.Revenue) / points.Sum(p => p.OrderCount), 2)
                : 0,
            Points = points
        };
    }

    public async Task<List<TopProductDto>> GetTopProductsAsync(int limit)
    {
        limit = limit <= 0 ? 10 : Math.Min(limit, 50);

        var rows = await _context.OrderItems
            .AsNoTracking()
            .Where(oi => oi.Order.Status != OrderStatus.Cancelled && oi.Order.Status != OrderStatus.Returned)
            .GroupBy(oi => new { oi.ProductId, oi.ProductName, oi.ProductImage })
            .Select(g => new
            {
                g.Key.ProductId,
                g.Key.ProductName,
                g.Key.ProductImage,
                UnitsSold = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.TotalPrice)
            })
            .OrderByDescending(x => x.Revenue)
            .Take(limit)
            .ToListAsync();

        if (rows.Count == 0) return new List<TopProductDto>();

        var productIds = rows.Select(r => r.ProductId).Distinct().ToList();
        var categories = await _context.Products
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Category })
            .ToListAsync();
        var catMap = categories.ToDictionary(p => p.Id, p => p.Category);

        return rows.Select(r => new TopProductDto
        {
            ProductId = r.ProductId,
            ProductName = r.ProductName,
            ImageUrl = r.ProductImage,
            Category = catMap.TryGetValue(r.ProductId, out var c) ? c : null,
            UnitsSold = r.UnitsSold,
            Revenue = r.Revenue
        }).ToList();
    }

    public async Task<List<CategoryBreakdownDto>> GetCategoryBreakdownAsync()
    {
        var rows = await _context.OrderItems
            .AsNoTracking()
            .Where(oi => oi.Order.Status != OrderStatus.Cancelled && oi.Order.Status != OrderStatus.Returned)
            .GroupBy(oi => 1)
            .Select(g => new
            {
                Items = g.ToList()
            })
            .FirstOrDefaultAsync();

        if (rows == null || rows.Items.Count == 0) return new List<CategoryBreakdownDto>();

        var productIds = rows.Items.Select(i => i.ProductId).Distinct().ToList();
        var categoryMap = await _context.Products
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Category })
            .ToDictionaryAsync(p => p.Id, p => p.Category);

        return rows.Items
            .GroupBy(i => categoryMap.TryGetValue(i.ProductId, out var c) ? c : "Uncategorized")
            .Select(g => new CategoryBreakdownDto
            {
                Category = g.Key,
                UnitsSold = g.Sum(x => x.Quantity),
                OrderCount = g.Select(x => x.OrderId).Distinct().Count(),
                Revenue = g.Sum(x => x.TotalPrice)
            })
            .OrderByDescending(x => x.Revenue)
            .ToList();
    }

    public async Task<List<OrderStatusBreakdownDto>> GetOrderStatusBreakdownAsync()
    {
        var rows = await _context.Orders
            .AsNoTracking()
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        return rows
            .Select(r => new OrderStatusBreakdownDto
            {
                Status = r.Status.ToString(),
                Count = r.Count
            })
            .OrderByDescending(x => x.Count)
            .ToList();
    }

    public async Task<List<LowStockProductDto>> GetLowStockProductsAsync(int threshold)
    {
        threshold = threshold <= 0 ? 10 : threshold;
        return await _context.Products
            .AsNoTracking()
            .Where(p => p.IsActive && p.Stock <= threshold)
            .OrderBy(p => p.Stock)
            .Take(20)
            .Select(p => new LowStockProductDto
            {
                ProductId = p.Id,
                ProductName = p.Name,
                Category = p.Category,
                ImageUrl = p.ProductImages
                    .OrderBy(pi => pi.SortOrder)
                    .Select(pi => pi.ImageUrl)
                    .FirstOrDefault(),
                Stock = p.Stock
            })
            .ToListAsync();
    }

    public async Task<AnalyticsOverviewDto> GetOverviewAsync()
    {
        var today = DateTime.UtcNow.Date;
        var totalProducts = await _context.Products.AsNoTracking().CountAsync();
        var totalOrders = await _context.Orders.AsNoTracking().CountAsync();
        var totalUsers = await _context.Users.AsNoTracking().CountAsync();
        var pendingOrders = await _context.Orders.AsNoTracking()
            .CountAsync(o => o.Status == OrderStatus.Pending);
        var pageViewsToday = await _context.PageViews.AsNoTracking()
            .CountAsync(pv => pv.CreatedAt >= today);
        var uniqueVisitorsToday = await _context.PageViews.AsNoTracking()
            .Where(pv => pv.CreatedAt >= today)
            .Select(pv => pv.IpAddress)
            .Distinct()
            .CountAsync();

        return new AnalyticsOverviewDto
        {
            TotalProducts = totalProducts,
            TotalOrders = totalOrders,
            TotalUsers = totalUsers,
            PendingOrders = pendingOrders,
            OrderStatusBreakdown = await GetOrderStatusBreakdownAsync(),
            TopProducts = await GetTopProductsAsync(5),
            LowStockProducts = await GetLowStockProductsAsync(10),
            PageViewsToday = pageViewsToday,
            UniqueVisitorsToday = uniqueVisitorsToday
        };
    }

    public async Task<PageViewSummaryDto> GetPageViewsAsync(string period)
    {
        period = (period ?? "7d").ToLowerInvariant();
        int days;
        switch (period)
        {
            case "24h": days = 1; break;
            case "30d": days = 30; break;
            default: days = 7; break;
        }

        var start = DateTime.UtcNow.Date.AddDays(-(days - 1));

        var allViews = await _context.PageViews
            .AsNoTracking()
            .Where(pv => pv.CreatedAt >= start)
            .Select(pv => new { pv.CreatedAt, pv.IpAddress })
            .ToListAsync();

        var points = Enumerable.Range(0, days).Select(i =>
        {
            var date = start.AddDays(i);
            var dayViews = allViews.Where(v => v.CreatedAt.Date == date).ToList();
            return new PageViewPointDto
            {
                Date = date,
                Label = date.ToString("dd MMM"),
                Count = dayViews.Count
            };
        }).ToList();

        var uniquePoints = Enumerable.Range(0, days).Select(i =>
        {
            var date = start.AddDays(i);
            var uniqueIps = allViews
                .Where(v => v.CreatedAt.Date == date)
                .Select(v => v.IpAddress)
                .Distinct()
                .Count();
            return new PageViewPointDto
            {
                Date = date,
                Label = date.ToString("dd MMM"),
                Count = uniqueIps
            };
        }).ToList();

        return new PageViewSummaryDto
        {
            Period = period,
            TotalViews = allViews.Count,
            UniqueVisitors = allViews.Select(v => v.IpAddress).Distinct().Count(),
            Views = points,
            UniqueVisitorsPoints = uniquePoints
        };
    }

    public async Task<List<TopPageDto>> GetTopPagesAsync(string period)
    {
        var start = GetPeriodStart(period);
        return await _context.PageViews
            .AsNoTracking()
            .Where(pv => pv.CreatedAt >= start)
            .GroupBy(pv => pv.Path)
            .Select(g => new TopPageDto { Path = g.Key, Count = g.Count() })
            .OrderByDescending(dto => dto.Count)
            .Take(20)
            .ToListAsync();
    }

    public async Task<List<TopSearchDto>> GetTopSearchesAsync(string period)
    {
        var start = GetPeriodStart(period);
        return await _context.UserActivities
            .AsNoTracking()
            .Where(ua => ua.Type == ActivityType.Search && ua.CreatedAt >= start)
            .GroupBy(ua => ua.Data)
            .Select(g => new TopSearchDto { Keyword = g.Key, Count = g.Count() })
            .OrderByDescending(dto => dto.Count)
            .Take(20)
            .ToListAsync();
    }

    private static DateTime GetPeriodStart(string period)
    {
        return period switch
        {
            "24h" => DateTime.UtcNow.AddHours(-24),
            "30d" => DateTime.UtcNow.Date.AddDays(-30),
            _ => DateTime.UtcNow.Date.AddDays(-7)
        };
    }
}
