using System.Security.Claims;
using EcomApi.DTOs;
using EcomApi.Models;
using EcomApi.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcomApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ActivityController : ControllerBase
{
    private readonly IActivityRepository _activityRepository;

    public ActivityController(IActivityRepository activityRepository)
    {
        _activityRepository = activityRepository;
    }

    [HttpPost]
    public async Task<ActionResult> LogActivity([FromBody] LogActivityDto dto)
    {
        if (!Enum.TryParse<ActivityType>(dto.Type, true, out var type))
            return BadRequest(new { error = $"Invalid activity type. Valid values: {string.Join(", ", Enum.GetNames<ActivityType>())}" });

        int? userId = null;
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userIdClaim))
            userId = int.Parse(userIdClaim);

        string? sessionId = null;
        if (!userId.HasValue && Request.Cookies.TryGetValue("CartId", out var sid))
            sessionId = sid;

        var activity = new UserActivity
        {
            UserId = userId,
            SessionId = sessionId,
            Type = type,
            Data = dto.Data
        };

        await _activityRepository.LogAsync(activity);
        return Ok(new { message = "Activity logged" });
    }
}
