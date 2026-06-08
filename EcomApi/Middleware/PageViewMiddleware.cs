using System.Net;
using EcomApi.Data;
using EcomApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EcomApi.Middleware;

public class PageViewMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly HashSet<string> _skipExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".js", ".css", ".png", ".jpg", ".jpeg", ".gif", ".svg", ".ico", ".webp",
        ".woff", ".woff2", ".ttf", ".eot", ".json", ".map"
    };

    public PageViewMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ApplicationDbContext db)
    {
        var path = context.Request.Path.Value ?? "";

        if (context.Request.Method == "GET"
            && !path.StartsWith("/api", StringComparison.OrdinalIgnoreCase)
            && !path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase)
            && !Path.HasExtension(path)
            && !_skipExtensions.Contains(Path.GetExtension(path)))
        {
            var ip = context.Connection.RemoteIpAddress?.ToString();
            if (ip != null && ip.Contains('.'))
            {
                var parts = ip.Split('.');
                parts[^1] = "0";
                if (parts.Length == 4) ip = string.Join(".", parts);
            }

            var userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            string? sessionId = null;
            try { sessionId = context.Session?.Id; } catch { }

            db.PageViews.Add(new PageView
            {
                Path = path.Length > 500 ? path[..500] : path,
                IpAddress = ip,
                UserAgent = context.Request.Headers.UserAgent.ToString()?[..Math.Min(500, context.Request.Headers.UserAgent.ToString().Length)],
                SessionId = sessionId,
                UserId = userId != null ? int.Parse(userId) : null,
                Referrer = context.Request.Headers.Referer.ToString()?[..Math.Min(1000, context.Request.Headers.Referer.ToString().Length)],
                CreatedAt = DateTime.UtcNow
            });

            try { await db.SaveChangesAsync(); } catch { }
        }

        await _next(context);
    }
}

public static class PageViewMiddlewareExtensions
{
    public static IApplicationBuilder UsePageViewMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<PageViewMiddleware>();
    }
}
