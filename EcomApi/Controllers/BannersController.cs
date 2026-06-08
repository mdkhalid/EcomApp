using EcomApi.DTOs;
using EcomApi.Models;
using EcomApi.Repositories;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcomApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BannersController : ControllerBase
{
    private readonly IBannerRepository _repository;

    public BannersController(IBannerRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BannerDto>>> GetAll(CancellationToken cancellationToken = default)
    {
        var banners = await _repository.GetAllAsync(cancellationToken);
        return Ok(banners.Adapt<List<BannerDto>>());
    }

    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<BannerDto>>> GetActive(CancellationToken cancellationToken = default)
    {
        var banners = await _repository.GetActiveAsync(cancellationToken);
        return Ok(banners.Adapt<List<BannerDto>>());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BannerDto>> GetById(int id, CancellationToken cancellationToken = default)
    {
        var banner = await _repository.GetByIdAsync(id);
        if (banner == null) return NotFound();
        return Ok(banner.Adapt<BannerDto>());
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<BannerDto>> Create([FromBody] CreateBannerDto createDto, CancellationToken cancellationToken = default)
    {
        var banner = createDto.Adapt<Banner>();
        var created = await _repository.AddAsync(banner);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created.Adapt<BannerDto>());
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<BannerDto>> Update(int id, [FromBody] UpdateBannerDto updateDto, CancellationToken cancellationToken = default)
    {
        var banner = await _repository.GetByIdAsync(id);
        if (banner == null) return NotFound();
        updateDto.Adapt(banner);
        var updated = await _repository.UpdateAsync(banner);
        return Ok(updated.Adapt<BannerDto>());
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var deleted = await _repository.DeleteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }

    [HttpPost("{id}/image")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<BannerDto>> UploadImage(int id, IFormFile file, CancellationToken cancellationToken = default)
    {
        var banner = await _repository.GetByIdAsync(id);
        if (banner == null) return NotFound();

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
            return BadRequest(new { error = "Invalid file type. Allowed: jpg, jpeg, png, webp." });

        if (file.Length > 5 * 1024 * 1024)
            return BadRequest(new { error = "File size exceeds 5MB limit." });

        var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        Directory.CreateDirectory(uploadsDir);

        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsDir, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        banner.ImageUrl = $"/uploads/{fileName}";
        var updated = await _repository.UpdateAsync(banner);
        return Ok(updated.Adapt<BannerDto>());
    }
}
