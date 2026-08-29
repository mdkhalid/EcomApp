namespace EcomApi.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        headers["Content-Security-Policy"] =
            "default-src 'self'; img-src 'self' data: https:; " +
            "style-src 'self' 'unsafe-inline'; script-src 'self'; " +
            "object-src 'none'; base-uri 'self'; frame-ancestors 'none'";
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

        await _next(context);
    }
}
