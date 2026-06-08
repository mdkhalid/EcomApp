using System.Security.Claims;
using EcomApi.DTOs;
using EcomApi.Models;
using EcomApi.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcomApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,SubAdmin")]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsRepository _analytics;

    public AnalyticsController(IAnalyticsRepository analytics)
    {
        _analytics = analytics;
    }

    private static bool IsSuperAdmin(ClaimsPrincipal user)
    {
        return user.FindFirstValue(ClaimTypes.Role) == UserRoles.Admin;
    }

    [HttpGet("overview")]
    public async Task<ActionResult<AnalyticsOverviewDto>> GetOverview()
    {
        var overview = await _analytics.GetOverviewAsync();
        if (!IsSuperAdmin(User))
        {
            overview.TopProducts = new List<TopProductDto>();
        }
        return Ok(overview);
    }

    [HttpGet("revenue")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<RevenueSummaryDto>> GetRevenue([FromQuery] string period = "monthly")
    {
        var revenue = await _analytics.GetRevenueAsync(period);
        return Ok(revenue);
    }

    [HttpGet("top-products")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<TopProductDto>>> GetTopProducts([FromQuery] int limit = 10)
    {
        return Ok(await _analytics.GetTopProductsAsync(limit));
    }

    [HttpGet("category-breakdown")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<CategoryBreakdownDto>>> GetCategoryBreakdown()
    {
        return Ok(await _analytics.GetCategoryBreakdownAsync());
    }

    [HttpGet("order-status")]
    public async Task<ActionResult<List<OrderStatusBreakdownDto>>> GetOrderStatusBreakdown()
    {
        return Ok(await _analytics.GetOrderStatusBreakdownAsync());
    }

    [HttpGet("low-stock")]
    public async Task<ActionResult<List<LowStockProductDto>>> GetLowStock([FromQuery] int threshold = 10)
    {
        return Ok(await _analytics.GetLowStockProductsAsync(threshold));
    }

    [HttpGet("page-views")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PageViewSummaryDto>> GetPageViews([FromQuery] string period = "7d")
    {
        return Ok(await _analytics.GetPageViewsAsync(period));
    }

    [HttpGet("top-pages")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<TopPageDto>>> GetTopPages([FromQuery] string period = "7d")
    {
        return Ok(await _analytics.GetTopPagesAsync(period));
    }

    [HttpGet("top-searches")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<TopSearchDto>>> GetTopSearches([FromQuery] string period = "7d")
    {
        return Ok(await _analytics.GetTopSearchesAsync(period));
    }
}
