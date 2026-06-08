using EcomApi.DTOs;

namespace EcomApi.Repositories;

public interface IAnalyticsRepository
{
    Task<RevenueSummaryDto> GetRevenueAsync(string period);
    Task<List<TopProductDto>> GetTopProductsAsync(int limit);
    Task<List<CategoryBreakdownDto>> GetCategoryBreakdownAsync();
    Task<List<OrderStatusBreakdownDto>> GetOrderStatusBreakdownAsync();
    Task<List<LowStockProductDto>> GetLowStockProductsAsync(int threshold);
    Task<AnalyticsOverviewDto> GetOverviewAsync();
    Task<PageViewSummaryDto> GetPageViewsAsync(string period);
    Task<List<TopPageDto>> GetTopPagesAsync(string period);
    Task<List<TopSearchDto>> GetTopSearchesAsync(string period);
}
