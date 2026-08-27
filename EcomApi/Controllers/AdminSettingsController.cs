using EcomApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcomApi.Controllers;

[ApiController]
[Route("api/admin/settings")]
[Authorize(Roles = "Admin")]
public class AdminSettingsController : ControllerBase
{
    private readonly ISettingsProvider _settings;

    public AdminSettingsController(ISettingsProvider settings)
    {
        _settings = settings;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken = default)
    {
        var result = new List<object>();
        foreach (var descriptor in SettingsCatalog.All)
        {
            var raw = await _settings.GetRawAsync(descriptor.Key, cancellationToken);
            var hasValue = !string.IsNullOrEmpty(raw);
            result.Add(new
            {
                descriptor.Key,
                descriptor.Group,
                descriptor.Description,
                descriptor.IsSensitive,
                Value = descriptor.IsSensitive ? (hasValue ? "********" : "") : raw,
                descriptor.DefaultValue
            });
        }
        return Ok(result);
    }

    [HttpPut]
    public async Task<IActionResult> Update(
        [FromBody] List<SettingUpdateDto> updates,
        CancellationToken cancellationToken = default)
    {
        foreach (var update in updates)
        {
            var descriptor = SettingsCatalog.Find(update.Key);
            if (descriptor == null)
                return BadRequest(new { error = $"Unknown setting key: {update.Key}" });

            await _settings.UpsertAsync(update.Key, update.Value, cancellationToken);
            await _settings.InvalidateAsync(update.Key, cancellationToken);
        }
        return Ok(new { message = "Settings updated." });
    }
}

public class SettingUpdateDto
{
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
}
