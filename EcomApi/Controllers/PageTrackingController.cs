using System.Security.Claims;
using EcomApi.DTOs;
using EcomApi.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcomApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PageTrackingController : ControllerBase
{
    private readonly IAnalyticsRepository _analytics;

    public PageTrackingController(IAnalyticsRepository analytics)
    {
        _analytics = analytics;
    }

    [HttpPost("track")]
    public async Task<IActionResult> TrackPageView([FromBody] TrackPageViewDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        if (ip != null && ip.Contains('.'))
        {
            var parts = ip.Split('.');
            parts[^1] = "0";
            if (parts.Length == 4) ip = string.Join(".", parts);
        }

        await _analytics.TrackPageViewAsync(new Models.PageView
        {
            Path = dto.Path.Length > 500 ? dto.Path[..500] : dto.Path,
            IpAddress = ip,
            UserAgent = Request.Headers.UserAgent.ToString()?[..Math.Min(500, Request.Headers.UserAgent.ToString().Length)],
            SessionId = HttpContext.Session?.Id,
            UserId = userId != null ? int.Parse(userId) : null,
            Referrer = dto.Referrer?[..Math.Min(1000, dto.Referrer.Length)],
            CreatedAt = DateTime.UtcNow
        });

        return Ok(new { tracked = true });
    }
}
