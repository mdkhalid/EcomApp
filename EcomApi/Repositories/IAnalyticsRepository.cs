using EcomApi.DTOs;
using EcomApi.Models;

namespace EcomApi.Repositories;

public interface IAnalyticsRepository
{
    Task<RevenueSummaryDto> GetRevenueAsync(string period, CancellationToken cancellationToken = default);
    Task<List<TopProductDto>> GetTopProductsAsync(int limit, CancellationToken cancellationToken = default);
    Task<List<CategoryBreakdownDto>> GetCategoryBreakdownAsync(CancellationToken cancellationToken = default);
    Task<List<OrderStatusBreakdownDto>> GetOrderStatusBreakdownAsync(CancellationToken cancellationToken = default);
    Task<List<LowStockProductDto>> GetLowStockProductsAsync(int threshold, CancellationToken cancellationToken = default);
    Task<AnalyticsOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default);
    Task<PageViewSummaryDto> GetPageViewsAsync(string period, CancellationToken cancellationToken = default);
    Task<List<TopPageDto>> GetTopPagesAsync(string period, CancellationToken cancellationToken = default);
    Task<List<TopSearchDto>> GetTopSearchesAsync(string period, CancellationToken cancellationToken = default);
    Task TrackPageViewAsync(PageView pageView);
}
