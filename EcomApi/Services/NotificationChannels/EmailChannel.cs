using EcomApi.Models;
using EcomApi.Repositories;
using EcomApi.Services;
using Microsoft.Extensions.Logging;

namespace EcomApi.Services.NotificationChannels;

public class EmailChannel : INotificationChannel
{
    private readonly IEmailService _emailService;
    private readonly IOrderRepository _orderRepository;
    private readonly IInvoiceService _invoiceService;
    private readonly ILogger<EmailChannel> _logger;

    public EmailChannel(
        IEmailService emailService,
        IOrderRepository orderRepository,
        IInvoiceService invoiceService,
        ILogger<EmailChannel> logger)
    {
        _emailService = emailService;
        _orderRepository = orderRepository;
        _invoiceService = invoiceService;
        _logger = logger;
    }

    public NotificationChannelType ChannelType => NotificationChannelType.Email;

    public async Task SendAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message.Email) || string.IsNullOrWhiteSpace(message.HtmlBody)) return;

        var attachments = await BuildInvoiceAttachmentAsync(message, cancellationToken);
        await _emailService.SendAsync(message.Email, message.Subject ?? "", message.HtmlBody, attachments, cancellationToken);
    }

    /// <summary>
    /// Regenerates the order invoice PDF on demand for confirmation emails (no PII stored at rest).
    /// A failure here must never block the confirmation email, so it degrades to a body-only send.
    /// </summary>
    private async Task<List<EmailAttachment>?> BuildInvoiceAttachmentAsync(NotificationMessage message, CancellationToken cancellationToken)
    {
        if (message.Type != NotificationType.OrderConfirmation || message.OrderId == null)
            return null;

        try
        {
            var order = await _orderRepository.GetByIdAsync(message.OrderId.Value, cancellationToken);
            if (order == null)
            {
                _logger.LogWarning("Could not load order {OrderId} for confirmation invoice; sending without attachment.", message.OrderId);
                return null;
            }

            var pdf = await _invoiceService.GenerateInvoicePdfAsync(order);
            return new List<EmailAttachment> { new($"invoice-{order.Id}.pdf", "application/pdf", pdf) };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate invoice for order {OrderId}; sending confirmation without attachment.", message.OrderId);
            return null;
        }
    }
}
