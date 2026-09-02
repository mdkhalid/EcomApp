using EcomApi.Data;
using EcomApi.Models;
using EcomApi.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EcomApi.Services;

/// <summary>
/// Builds and sends the daily admin digest email.
/// </summary>
public class DailyDigestRunner
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationQueue _notificationQueue;
    private readonly ISettingsProvider _settings;
    private readonly ILogger<DailyDigestRunner> _logger;

    public DailyDigestRunner(
        ApplicationDbContext context,
        INotificationQueue notificationQueue,
        ISettingsProvider settings,
        ILogger<DailyDigestRunner> logger)
    {
        _context = context;
        _notificationQueue = notificationQueue;
        _settings = settings;
        _logger = logger;
    }

    public async Task<bool> RunAsync(CancellationToken cancellationToken = default)
    {
        var enabled = await _settings.GetAsync("Digest:Enabled", false, cancellationToken);
        if (!enabled)
        {
            _logger.LogDebug("Daily digest is disabled; skipping.");
            return false;
        }

        var recipient = await _settings.GetRawAsync("Digest:Recipient", cancellationToken);
        if (string.IsNullOrWhiteSpace(recipient))
        {
            _logger.LogWarning("Daily digest enabled but no recipient configured (Digest:Recipient).");
            return false;
        }

        var lowStockThreshold = await _settings.GetAsync("Digest:LowStockThreshold", 10, cancellationToken);

        var yesterday = DateTime.UtcNow.Date.AddDays(-1);
        var yesterdayEnd = yesterday.AddDays(1);

        var ordersYesterday = await _context.Orders
            .AsNoTracking()
            .Where(o => o.CreatedAt >= yesterday && o.CreatedAt < yesterdayEnd
                && o.Status != OrderStatus.Cancelled && o.Status != OrderStatus.Returned)
            .ToListAsync(cancellationToken);

        var ordersCount = ordersYesterday.Count;
        var ordersRevenue = ordersYesterday.Sum(o => o.TotalAmount);

        var newUsersYesterday = await _context.Users
            .AsNoTracking()
            .Where(u => u.CreatedAt >= yesterday && u.CreatedAt < yesterdayEnd)
            .CountAsync(cancellationToken);

        var pendingReturns = await _context.ReturnRequests
            .AsNoTracking()
            .Where(r => r.Status == ReturnStatus.Requested || r.Status == ReturnStatus.Approved)
            .CountAsync(cancellationToken);

        var lowStockProducts = await _context.Products
            .AsNoTracking()
            .Where(p => p.IsActive && p.Stock <= lowStockThreshold)
            .OrderBy(p => p.Stock)
            .Take(10)
            .Select(p => new { p.Name, p.Stock })
            .ToListAsync(cancellationToken);

        var lowStockList = lowStockProducts.Select(p => (p.Name, p.Stock)).ToList();

        var html = EmailTemplates.DailyDigest(yesterday, ordersCount, ordersRevenue, newUsersYesterday, pendingReturns, lowStockList);

        var message = new NotificationMessage
        {
            Type = NotificationType.Welcome,
            Email = recipient,
            Subject = $"Ecom Daily Digest — {yesterday:dd MMM yyyy}",
            HtmlBody = html,
            TextBody = $"Daily Digest for {yesterday:dd MMM yyyy}: {ordersCount} orders, ₹{ordersRevenue:N2} revenue, {newUsersYesterday} new users, {pendingReturns} pending returns."
        };

        try
        {
            await _notificationQueue.EnqueueAsync(message, cancellationToken);
            _logger.LogInformation("Daily digest queued for {Recipient} (orders: {Orders}, revenue: {Revenue:N2}, new users: {Users}, pending returns: {Returns}).",
                recipient, ordersCount, ordersRevenue, newUsersYesterday, pendingReturns);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue daily digest for {Recipient}.", recipient);
            return false;
        }
    }
}