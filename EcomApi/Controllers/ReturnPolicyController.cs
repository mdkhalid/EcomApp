using System.Security.Claims;
using EcomApi.DTOs;
using EcomApi.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcomApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReturnPolicyController : ControllerBase
{
    private readonly IReturnPolicyRepository _returnPolicyRepository;

    public ReturnPolicyController(IReturnPolicyRepository returnPolicyRepository)
    {
        _returnPolicyRepository = returnPolicyRepository;
    }

    [HttpGet]
    public async Task<ActionResult<ReturnPolicyDto>> Get(CancellationToken cancellationToken = default)
    {
        var policy = await _returnPolicyRepository.GetAsync();
        if (policy == null)
            return Ok(new ReturnPolicyDto
            {
                ReturnWindowDays = 7,
                IsActive = true,
                PolicyText = "No return policy configured.",
                UpdatedAt = DateTime.UtcNow
            });

        return Ok(new ReturnPolicyDto
        {
            ReturnWindowDays = policy.ReturnWindowDays,
            IsActive = policy.IsActive,
            PolicyText = policy.PolicyText,
            UpdatedAt = policy.UpdatedAt,
            UpdatedBy = policy.UpdatedBy
        });
    }

    [HttpPut]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ReturnPolicyDto>> Update([FromBody] UpdateReturnPolicyDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.ReturnWindowDays < 0)
            return BadRequest(new { error = "Return window must be 0 or more days." });

        if (string.IsNullOrWhiteSpace(dto.PolicyText))
            return BadRequest(new { error = "Policy text is required." });

        var userName = User.FindFirstValue(ClaimTypes.Name)
                       ?? User.FindFirstValue(ClaimTypes.Email)
                       ?? "Admin";

        var policy = await _returnPolicyRepository.CreateOrUpdateAsync(new()
        {
            ReturnWindowDays = dto.ReturnWindowDays,
            IsActive = dto.IsActive,
            PolicyText = dto.PolicyText,
            UpdatedBy = userName
        });

        return Ok(new ReturnPolicyDto
        {
            ReturnWindowDays = policy.ReturnWindowDays,
            IsActive = policy.IsActive,
            PolicyText = policy.PolicyText,
            UpdatedAt = policy.UpdatedAt,
            UpdatedBy = policy.UpdatedBy
        });
    }
}
