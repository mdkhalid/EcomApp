namespace EcomApi.DTOs;

public class RevenuePointDto
{
    public string Label { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal Revenue { get; set; }
    public int OrderCount { get; set; }
}

public class RevenueSummaryDto
{
    public decimal TotalRevenue { get; set; }
    public int TotalOrders { get; set; }
    public decimal AverageOrderValue { get; set; }
    public string Period { get; set; } = "monthly";
    public List<RevenuePointDto> Points { get; set; } = new();
}

public class TopProductDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? Category { get; set; }
    public int UnitsSold { get; set; }
    public decimal Revenue { get; set; }
}

public class CategoryBreakdownDto
{
    public string Category { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public int UnitsSold { get; set; }
    public decimal Revenue { get; set; }
}

public class OrderStatusBreakdownDto
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class LowStockProductDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? ImageUrl { get; set; }
    public int Stock { get; set; }
}

public class PageViewPointDto
{
    public DateTime Date { get; set; }
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class PageViewSummaryDto
{
    public string Period { get; set; } = "daily";
    public int TotalViews { get; set; }
    public int UniqueVisitors { get; set; }
    public List<PageViewPointDto> Views { get; set; } = new();
    public List<PageViewPointDto> UniqueVisitorsPoints { get; set; } = new();
}

public class TopPageDto
{
    public string Path { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class TopSearchDto
{
    public string Keyword { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class AnalyticsOverviewDto
{
    public int TotalProducts { get; set; }
    public int TotalOrders { get; set; }
    public int TotalUsers { get; set; }
    public int PendingOrders { get; set; }
    public List<OrderStatusBreakdownDto> OrderStatusBreakdown { get; set; } = new();
    public List<TopProductDto> TopProducts { get; set; } = new();
    public List<LowStockProductDto> LowStockProducts { get; set; } = new();
    public int PageViewsToday { get; set; }
    public int UniqueVisitorsToday { get; set; }
}
