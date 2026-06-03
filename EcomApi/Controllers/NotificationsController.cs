using EcomApi.DTOs;
using EcomApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcomApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet("count")]
    public async Task<ActionResult<object>> GetUnreadCount()
    {
        var count = await _notificationService.GetUnreadNotificationCountAsync();
        return Ok(new { count });
    }

    [HttpGet]
    public async Task<ActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var notifications = await _notificationService.GetNotificationsAsync(page, pageSize);
        return Ok(new { items = notifications, page, pageSize });
    }

    [HttpPut("{id}/read")]
    public async Task<ActionResult> MarkRead(int id)
    {
        await _notificationService.MarkNotificationReadAsync(id);
        return Ok(new { message = "Marked as read" });
    }

    [HttpPut("read-all")]
    public async Task<ActionResult> MarkAllRead()
    {
        await _notificationService.MarkAllNotificationsReadAsync();
        return Ok(new { message = "All notifications marked as read" });
    }
}
