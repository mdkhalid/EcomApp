using System.Security.Claims;
using EcomApi.DTOs;
using EcomApi.Models;
using EcomApi.Repositories;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcomApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CouponsController : ControllerBase
{
    private readonly ICouponRepository _repository;

    public CouponsController(ICouponRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<CouponDto>>> GetAll(CancellationToken cancellationToken = default)
    {
        var coupons = await _repository.GetAllAsync();
        return Ok(coupons.Adapt<List<CouponDto>>());
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CouponDto>> GetById(int id, CancellationToken cancellationToken = default)
    {
        var coupon = await _repository.GetByIdAsync(id);
        if (coupon == null)
            return NotFound();
        return Ok(coupon.Adapt<CouponDto>());
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CouponDto>> Create([FromBody] CreateCouponDto createDto, CancellationToken cancellationToken = default)
    {
        var existing = await _repository.GetByCodeAsync(createDto.Code);
        if (existing != null)
            return BadRequest(new { error = "A coupon with this code already exists." });

        var coupon = createDto.Adapt<Coupon>();
        coupon.CreatedAt = DateTime.UtcNow;
        coupon.UpdatedAt = DateTime.UtcNow;
        var created = await _repository.AddAsync(coupon);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created.Adapt<CouponDto>());
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CouponDto>> Update(int id, [FromBody] UpdateCouponDto updateDto, CancellationToken cancellationToken = default)
    {
        var coupon = await _repository.GetByIdAsync(id);
        if (coupon == null)
            return NotFound();

        var existing = await _repository.GetByCodeAsync(updateDto.Code);
        if (existing != null && existing.Id != id)
            return BadRequest(new { error = "A coupon with this code already exists." });

        coupon.Code = updateDto.Code;
        coupon.Description = updateDto.Description;
        coupon.Type = Enum.Parse<CouponType>(updateDto.Type);
        coupon.Value = updateDto.Value;
        coupon.MinCartValue = updateDto.MinCartValue;
        coupon.MaxUses = updateDto.MaxUses;
        coupon.ExpiresAt = updateDto.ExpiresAt;
        coupon.IsActive = updateDto.IsActive;
        coupon.UpdatedAt = DateTime.UtcNow;

        var updated = await _repository.UpdateAsync(coupon);
        return Ok(updated.Adapt<CouponDto>());
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var deleted = await _repository.DeleteAsync(id);
        if (!deleted)
            return NotFound();
        return NoContent();
    }

    [HttpPost("validate")]
    [Authorize]
    public async Task<ActionResult<ValidateCouponResponse>> Validate([FromBody] ValidateCouponRequest request, CancellationToken cancellationToken = default)
    {
        int? userId = null;
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && int.TryParse(userIdClaim, out var uid))
            userId = uid;

        var result = await _repository.ValidateAndCalculateAsync(request.Code, request.CartTotal, userId);
        if (!result.IsValid)
            return BadRequest(result);
        return Ok(result);
    }
}
