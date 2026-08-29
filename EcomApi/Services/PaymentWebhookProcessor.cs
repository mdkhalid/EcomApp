using EcomApi.Data;
using EcomApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EcomApi.Services;

/// <summary>
/// Applies the outcome of a gateway event (succeeded / refunded) to the order and
/// payment records. Shared by the real Stripe webhook and the dev mock-confirm so
/// both paths behave identically. Safe to call more than once (idempotent).
/// </summary>
public class PaymentWebhookProcessor
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ILogger<PaymentWebhookProcessor> _logger;

    public PaymentWebhookProcessor(
        ApplicationDbContext context,
        INotificationService notificationService,
        ILogger<PaymentWebhookProcessor> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task ProcessSucceededAsync(int orderId, string? gatewayPaymentId = null, CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order == null)
        {
            _logger.LogWarning("Webhook succeeded for unknown order {OrderId}", orderId);
            return;
        }

        if (order.PaymentStatus == PaymentStatus.Succeeded && order.Status == OrderStatus.Paid)
            return; // idempotent

        var payment = order.Payments.FirstOrDefault(p =>
            (gatewayPaymentId == null || p.GatewayPaymentId == gatewayPaymentId) && p.Status == PaymentStatus.Pending)
            ?? order.Payments.LastOrDefault();

        if (payment != null)
        {
            payment.Status = PaymentStatus.Succeeded;
            payment.UpdatedAt = DateTime.UtcNow;
        }

        order.PaymentStatus = PaymentStatus.Succeeded;
        order.Status = OrderStatus.Paid;
        order.UpdatedAt = DateTime.UtcNow;
        order.StatusHistory.Add(new OrderStatusHistory
        {
            Status = OrderStatus.Paid,
            Note = "Payment received",
            Location = "Payment gateway",
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);

        await _notificationService.SendOrderConfirmationAsync(order);
        _logger.LogInformation("Order {OrderId} marked Paid", orderId);
    }

    public async Task ProcessRefundedAsync(int orderId, decimal refundAmount, CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order == null) return;

        var payment = order.Payments.LastOrDefault();
        if (payment != null)
        {
            payment.Status = refundAmount >= payment.Amount ? PaymentStatus.Refunded : PaymentStatus.PartiallyRefunded;
            payment.UpdatedAt = DateTime.UtcNow;
        }

        order.PaymentStatus = payment?.Status ?? PaymentStatus.Refunded;
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Order {OrderId} refunded (amount {Amount})", orderId, refundAmount);
        // A refund email can be queued here via INotificationService if a template exists.
    }
}
