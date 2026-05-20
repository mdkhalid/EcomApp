using System.Collections.Concurrent;
using System.Net;

namespace EcomApi.Middleware;

public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitMiddleware> _logger;
    private static readonly ConcurrentDictionary<string, RequestLog> _requestLogs = new();
    private static readonly TimeSpan _window = TimeSpan.FromMinutes(1);
    private const int MaxRequests = 30;

    public RateLimitMiddleware(RequestDelegate next, ILogger<RateLimitMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/api/auth"))
        {
            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var now = DateTime.UtcNow;

            var log = _requestLogs.GetOrAdd(ipAddress, _ => new RequestLog());

            lock (log)
            {
                log.Requests = log.Requests.Where(r => r > now.Subtract(_window)).ToList();

                if (log.Requests.Count >= MaxRequests)
                {
                    _logger.LogWarning("Rate limit exceeded for IP: {IpAddress}", ipAddress);
                    context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                    context.Response.ContentType = "application/json";
                    context.Response.WriteAsync(
                        "{\"error\":\"Too many requests. Please try again later.\"}").Wait();
                    return;
                }

                log.Requests.Add(now);
            }
        }

        await _next(context);
    }

    private class RequestLog
    {
        public List<DateTime> Requests { get; set; } = new();
    }
}
