using System.Security.Claims;
using EcomApi.DTOs;
using EcomApi.Models;
using EcomApi.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcomApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReturnsController : ControllerBase
{
    private readonly IReturnRepository _returnRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IReturnPolicyRepository _returnPolicyRepository;

    public ReturnsController(IReturnRepository returnRepository, IOrderRepository orderRepository, IReturnPolicyRepository returnPolicyRepository)
    {
        _returnRepository = returnRepository;
        _orderRepository = orderRepository;
        _returnPolicyRepository = returnPolicyRepository;
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ReturnRequestDto>> CreateReturnRequest([FromBody] CreateReturnRequestDto dto, CancellationToken cancellationToken = default)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var role = User.FindFirstValue(ClaimTypes.Role);
        if (role == "Admin")
            return Forbid();

        if (!Enum.TryParse<ReturnReason>(dto.Reason, true, out var reason))
            return BadRequest(new { error = $"Invalid reason. Valid values: {string.Join(", ", Enum.GetNames<ReturnReason>())}" });

        var order = await _orderRepository.GetByIdAsync(dto.OrderId);
        if (order == null || order.UserId != userId)
            return NotFound(new { error = "Order not found." });

        if (order.Status != OrderStatus.Delivered)
            return BadRequest(new { error = "You can only request a return for delivered orders." });

        var policy = await _returnPolicyRepository.GetAsync();
        if (policy != null && policy.IsActive && policy.ReturnWindowDays > 0 && order.ActualDeliveryDate.HasValue)
        {
            var daysSinceDelivery = (DateTime.UtcNow - order.ActualDeliveryDate.Value).TotalDays;
            if (daysSinceDelivery > policy.ReturnWindowDays)
                return BadRequest(new { error = $"Return window has expired. You can return within {policy.ReturnWindowDays} days of delivery." });
        }

        var hasPending = await _returnRepository.HasPendingReturnAsync(dto.OrderId, userId);
        if (hasPending)
            return BadRequest(new { error = "You already have a pending return request for this order." });

        var existing = await _returnRepository.GetByOrderIdAsync(dto.OrderId, userId);
        if (existing != null)
            return BadRequest(new { error = "Return request for this order already exists." });

        var returnRequest = new ReturnRequest
        {
            OrderId = dto.OrderId,
            UserId = userId,
            Reason = reason,
            Comment = dto.Comment,
            Status = ReturnStatus.Requested
        };

        returnRequest = await _returnRepository.CreateAsync(returnRequest);

        return CreatedAtAction(nameof(GetById), new { id = returnRequest.Id }, MapReturnRequest(returnRequest));
    }

    [HttpGet("my-returns")]
    [Authorize]
    public async Task<ActionResult<List<ReturnRequestDto>>> GetMyReturns(CancellationToken cancellationToken = default)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var returns = await _returnRepository.GetByUserIdAsync(userId);
        return Ok(returns.Select(MapReturnRequest).ToList());
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<ReturnRequestDto>> GetById(int id, CancellationToken cancellationToken = default)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var role = User.FindFirstValue(ClaimTypes.Role);

        var returnRequest = await _returnRepository.GetByIdAsync(id);
        if (returnRequest == null)
            return NotFound();

        if (returnRequest.UserId != userId && role != "Admin")
            return NotFound();

        return Ok(MapReturnRequest(returnRequest));
    }

    [HttpGet("order/{orderId}")]
    [Authorize]
    public async Task<ActionResult<ReturnRequestDto?>> GetByOrder(int orderId, CancellationToken cancellationToken = default)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var role = User.FindFirstValue(ClaimTypes.Role);

        if (role == "Admin")
        {
            var all = await _returnRepository.GetByUserIdAsync(userId);
            var adminResult = all.FirstOrDefault(r => r.OrderId == orderId);
            if (adminResult == null) return Ok(null);
            return Ok(MapReturnRequest(adminResult));
        }

        var returnRequest = await _returnRepository.GetByOrderIdAsync(orderId, userId);
        if (returnRequest == null)
            return Ok(null);

        return Ok(MapReturnRequest(returnRequest));
    }

    [HttpPut("{id}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ReturnRequestDto>> UpdateStatus(int id, [FromBody] UpdateReturnStatusDto dto, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<ReturnStatus>(dto.Status, true, out var status))
            return BadRequest(new { error = $"Invalid status. Valid values: {string.Join(", ", Enum.GetNames<ReturnStatus>())}" });

        if (status == ReturnStatus.Requested)
            return BadRequest(new { error = "Cannot set status back to Requested." });

        var returnRequest = await _returnRepository.UpdateStatusAsync(id, status, dto.AdminNote);
        if (returnRequest == null)
            return NotFound();

        if (status == ReturnStatus.Approved)
        {
            await _orderRepository.UpdateStatusAsync(returnRequest.OrderId, OrderStatus.Returned, "Return request approved by admin");
        }

        return Ok(MapReturnRequest(returnRequest));
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var (items, totalCount) = await _returnRepository.GetAllAsync(pageNumber, pageSize, status);

        return Ok(new
        {
            items = items.Select(MapReturnRequest),
            totalCount,
            pageNumber,
            pageSize
        });
    }

    private static ReturnRequestDto MapReturnRequest(ReturnRequest r)
    {
        return new ReturnRequestDto
        {
            Id = r.Id,
            OrderId = r.OrderId,
            UserId = r.UserId,
            UserName = r.User?.Username ?? "Unknown",
            Reason = r.Reason.ToString(),
            Comment = r.Comment,
            Status = r.Status.ToString(),
            AdminNote = r.AdminNote,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        };
    }
}
