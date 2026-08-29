using System.Security.Claims;
using EcomApi.Data;
using EcomApi.DTOs;
using EcomApi.Models;
using EcomApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcomApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly PaymentGatewayFactory _gatewayFactory;
    private readonly PaymentWebhookProcessor _processor;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        ApplicationDbContext context,
        PaymentGatewayFactory gatewayFactory,
        PaymentWebhookProcessor processor,
        IConfiguration configuration,
        ILogger<PaymentsController> logger)
    {
        _context = context;
        _gatewayFactory = gatewayFactory;
        _processor = processor;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet("config")]
    [AllowAnonymous]
    public IActionResult GetConfig()
    {
        var gateway = _gatewayFactory.GetGateway().Gateway;
        return Ok(new PaymentConfigDto
        {
            Gateway = gateway.ToString(),
            PublishableKey = _configuration["Stripe:PublishableKey"]
        });
    }

    [HttpPost("intent")]
    [Authorize]
    public async Task<IActionResult> CreateIntent([FromBody] CreatePaymentIntentDto dto, CancellationToken cancellationToken = default)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var order = await _context.Orders
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.Id == dto.OrderId, cancellationToken);

        if (order == null)
            return NotFound(new { error = "Order not found." });
        if (order.UserId != userId && User.FindFirstValue(ClaimTypes.Role) != "Admin")
            return Forbid();
        if (order.PaymentStatus == PaymentStatus.Succeeded)
            return BadRequest(new { error = "Order is already paid." });
        if (order.Status != OrderStatus.AwaitingPayment)
            return BadRequest(new { error = "Order is not awaiting payment." });

        var existing = order.Payments.FirstOrDefault(p => p.Status == PaymentStatus.Pending);
        if (existing != null)
            return Ok(new { clientSecret = existing.ClientSecret, gatewayPaymentId = existing.GatewayPaymentId, gateway = existing.Gateway.ToString() });

        var gateway = _gatewayFactory.GetGateway();
        var idempotencyKey = $"order-{order.Id}";
        var result = await gateway.CreatePaymentIntentAsync(new CreatePaymentRequest(
            order.Id, order.TotalAmount, order.TotalAmount > 0 ? "inr" : "inr", idempotencyKey, order.CustomerEmail), cancellationToken);

        if (!result.Success)
            return StatusCode(502, new { error = result.Error ?? "Failed to create payment intent." });

        var payment = new Payment
        {
            OrderId = order.Id,
            Gateway = gateway.Gateway,
            GatewayPaymentId = result.GatewayPaymentId!,
            ClientSecret = result.ClientSecret,
            Amount = order.TotalAmount,
            Currency = "INR",
            Status = PaymentStatus.Pending,
            IdempotencyKey = idempotencyKey
        };
        _context.Payments.Add(payment);
        order.PaymentGateway = gateway.Gateway;
        order.PaymentIntentId = result.GatewayPaymentId;
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new { clientSecret = result.ClientSecret, gatewayPaymentId = result.GatewayPaymentId, gateway = gateway.Gateway.ToString() });
    }

    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook(CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(cancellationToken);
        var signature = Request.Headers["Stripe-Signature"].ToString();

        var gateway = _gatewayFactory.GetGateway();
        var evt = await gateway.ParseWebhookAsync(body, signature, cancellationToken);
        if (evt == null)
            return BadRequest(new { error = "Invalid webhook." });

        if (await _context.ProcessedWebhookEvents.AnyAsync(p => p.EventId == evt.EventId, cancellationToken))
            return Ok(); // already processed — idempotent

        if (evt.OrderId == null)
            return Ok();

        if (evt.Succeeded)
            await _processor.ProcessSucceededAsync(evt.OrderId.Value, evt.GatewayPaymentId, cancellationToken);
        else if (evt.Refunded)
            await _processor.ProcessRefundedAsync(evt.OrderId.Value, evt.RefundAmount ?? 0, cancellationToken);

        _context.ProcessedWebhookEvents.Add(new ProcessedWebhookEvent
        {
            EventId = evt.EventId,
            Gateway = gateway.Gateway.ToString(),
            RawJson = body.Length > 4000 ? body[..4000] : body
        });
        await _context.SaveChangesAsync(cancellationToken);

        return Ok();
    }

    [HttpPost("mock-confirm")]
    [Authorize]
    public async Task<IActionResult> MockConfirm([FromBody] MockConfirmDto dto, CancellationToken cancellationToken = default)
    {
        if (_gatewayFactory.GetGateway().Gateway != PaymentGateway.Mock)
            return BadRequest(new { error = "Mock confirmation is only available in mock payment mode." });

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == dto.OrderId, cancellationToken);
        if (order == null)
            return NotFound(new { error = "Order not found." });
        if (order.UserId != userId && User.FindFirstValue(ClaimTypes.Role) != "Admin")
            return Forbid();

        await _processor.ProcessSucceededAsync(order.Id, cancellationToken: cancellationToken);
        return Ok(new { message = "Payment confirmed (mock)." });
    }

    [HttpPost("refund")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Refund([FromBody] RefundOrderDto dto, CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.Id == dto.OrderId, cancellationToken);
        if (order == null)
            return NotFound(new { error = "Order not found." });

        var payment = order.Payments.LastOrDefault(p => p.Status == PaymentStatus.Succeeded);
        if (payment == null)
            return BadRequest(new { error = "No successful payment to refund." });

        var gateway = _gatewayFactory.GetGateway();
        var amount = dto.Amount ?? payment.Amount;
        var result = await gateway.RefundAsync(payment.GatewayPaymentId, amount, "requested_by_customer", cancellationToken);
        if (!result.Success)
            return StatusCode(502, new { error = result.Error ?? "Refund failed." });

        await _processor.ProcessRefundedAsync(order.Id, amount, cancellationToken);
        return Ok(new { message = "Refund issued.", gatewayRefundId = result.GatewayRefundId });
    }
}
