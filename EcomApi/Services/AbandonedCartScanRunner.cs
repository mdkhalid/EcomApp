using EcomApi.Data;
using EcomApi.Models;
using EcomApi.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EcomApi.Services;

/// <summary>
/// One pass of the abandoned-cart scan. Finds carts untouched past the threshold,
/// respects opt-out and per-user cooldown, optionally creates a single-use coupon,
/// and enqueues a recovery email through the existing notification pipeline.
/// </summary>
public class AbandonedCartScanRunner
{
    private readonly ApplicationDbContext _context;
    private readonly ICouponRepository _couponRepository;
    private readonly INotificationQueue _notificationQueue;
    private readonly ISettingsProvider _settings;
    private readonly ILogger<AbandonedCartScanRunner> _logger;

    public AbandonedCartScanRunner(
        ApplicationDbContext context,
        ICouponRepository couponRepository,
        INotificationQueue notificationQueue,
        ISettingsProvider settings,
        ILogger<AbandonedCartScanRunner> logger)
    {
        _context = context;
        _couponRepository = couponRepository;
        _notificationQueue = notificationQueue;
        _settings = settings;
        _logger = logger;
    }

    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        var abandonmentHours = await _settings.GetAsync("Cart:AbandonmentHours", 24, cancellationToken);
        var resendDays = await _settings.GetAsync("Cart:ResendDays", 7, cancellationToken);
        var attachCoupon = await _settings.GetAsync("Cart:RecoveryCouponEnabled", false, cancellationToken);
        var couponAmount = await _settings.GetAsync("Cart:RecoveryCouponAmount", 100m, cancellationToken);
        var minCartTotal = await _settings.GetAsync("Cart:MinCartTotal", 100m, cancellationToken);

        var threshold = DateTime.UtcNow.AddHours(-abandonmentHours);
        var cooldownCutoff = DateTime.UtcNow.AddDays(-resendDays);

        var candidates = await _context.Carts
            .Include(c => c.Items)
            .Where(c => c.UserId != null
                        && c.UpdatedAt < threshold
                        && c.Items.Any())
            .ToListAsync(cancellationToken);

        if (candidates.Count == 0)
            return 0;

        var sent = 0;
        var baseUrl = _settings.GetBaseUrl();

        foreach (var cart in candidates)
        {
            if (cancellationToken.IsCancellationRequested) break;

            var user = cart.User ?? await _context.Users.FindAsync(new object?[] { cart.UserId }, cancellationToken);
            if (user == null || !user.IsActive || user.IsLockedOut) continue;
            if (user.AbandonedCartOptOut) continue;
            if (user.LastAbandonedCartNotifiedAt.HasValue
                && user.LastAbandonedCartNotifiedAt.Value > cooldownCutoff) continue;

            var cartTotal = cart.TotalAmount;
            if (cartTotal < minCartTotal) continue;

            string? couponCode = null;
            DateTime? couponExpiresAt = null;
            decimal issuedCouponAmount = 0;

            if (attachCoupon && couponAmount > 0)
            {
                couponCode = $"COMEBACK-{user.Id}-{(uint)cart.Id:X}";
                var coupon = new Coupon
                {
                    Code = couponCode,
                    Description = "One-time comeback discount — abandoned cart recovery",
                    Type = CouponType.FixedAmount,
                    Value = couponAmount,
                    MinCartValue = 0,
                    MaxUses = 1,
                    ExpiresAt = DateTime.UtcNow.AddDays(resendDays),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                try
                {
                    await _couponRepository.AddAsync(coupon, cancellationToken);
                    couponExpiresAt = coupon.ExpiresAt;
                    issuedCouponAmount = coupon.Value;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not create recovery coupon for user {UserId}; sending email without coupon.", user.Id);
                    couponCode = null;
                    issuedCouponAmount = 0;
                    couponExpiresAt = null;
                }
            }

            var unsubscribeToken = Convert.ToBase64String(
                System.Security.Cryptography.SHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes($"{user.Id}-abandoned-cart-opt-out-{DateTime.UtcNow:yyyyMMdd}")));
            var unsubscribeLink = $"{baseUrl}/api/auth/abandoned-cart/unsubscribe?token={unsubscribeToken}";

            var cartUrl = $"{baseUrl}/cart";

            var message = new NotificationMessage
            {
                Type = NotificationType.AbandonedCart,
                Email = user.Email,
                Subject = "You left items in your cart",
                HtmlBody = EmailTemplates.AbandonedCart(
                    user.FirstName ?? user.Username,
                    cartUrl,
                    couponCode,
                    issuedCouponAmount,
                    couponExpiresAt,
                    unsubscribeLink),
                TextBody = $"You left items in your cart. Resume checkout: {cartUrl}"
            };

            try
            {
                await _notificationQueue.EnqueueAsync(message, cancellationToken);
                user.LastAbandonedCartNotifiedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
                sent++;
                _logger.LogInformation("Abandoned-cart recovery email queued for {Email} (cart {CartId}, items {ItemCount}).",
                    user.Email, cart.Id, cart.Items.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enqueue abandoned-cart email for user {UserId}.", user.Id);
            }
        }

        return sent;
    }
}