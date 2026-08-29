using EcomApi.Data;
using EcomApi.Models;
using EcomApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcomApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ShippingController : ControllerBase
{
    private readonly PricingService _pricing;
    private readonly ApplicationDbContext _context;

    public ShippingController(PricingService pricing, ApplicationDbContext context)
    {
        _pricing = pricing;
        _context = context;
    }

    /// <summary>Public estimate used by the checkout UI. Recomputes server-side.</summary>
    [HttpPost("quote")]
    [AllowAnonymous]
    public async Task<IActionResult> Quote([FromBody] ShippingQuoteRequestDto dto, CancellationToken cancellationToken = default)
    {
        var breakdown = await _pricing.ComputeTotalsAsync(dto.Subtotal, dto.Discount, dto.City, dto.Zip, cancellationToken);
        return Ok(new
        {
            shipping = breakdown.Shipping,
            tax = breakdown.Tax,
            taxName = breakdown.TaxName,
            taxPercentage = breakdown.TaxPercentage,
            total = breakdown.Total
        });
    }

    [HttpGet("zones")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetZones(CancellationToken cancellationToken = default)
    {
        var zones = await _context.ShippingZones.Include(z => z.Rates).ToListAsync(cancellationToken);
        return Ok(zones);
    }

    [HttpPost("zones")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateZone([FromBody] ShippingZone zone, CancellationToken cancellationToken = default)
    {
        _context.ShippingZones.Add(zone);
        await _context.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetZones), new { id = zone.Id }, zone);
    }

    [HttpPut("zones/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateZone(int id, [FromBody] ShippingZone zone, CancellationToken cancellationToken = default)
    {
        if (id != zone.Id) return BadRequest();
        var existing = await _context.ShippingZones.Include(z => z.Rates).FirstOrDefaultAsync(z => z.Id == id, cancellationToken);
        if (existing == null) return NotFound();

        existing.Name = zone.Name;
        existing.Regions = zone.Regions;
        _context.ShippingRates.RemoveRange(existing.Rates);
        existing.Rates = zone.Rates;
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(existing);
    }

    [HttpDelete("zones/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteZone(int id, CancellationToken cancellationToken = default)
    {
        var zone = await _context.ShippingZones.FindAsync(id);
        if (zone == null) return NotFound();
        _context.ShippingZones.Remove(zone);
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("tax-rates")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetTaxRates(CancellationToken cancellationToken = default)
    {
        return Ok(await _context.TaxRates.ToListAsync(cancellationToken));
    }

    [HttpPost("tax-rates")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateTaxRate([FromBody] TaxRate rate, CancellationToken cancellationToken = default)
    {
        _context.TaxRates.Add(rate);
        await _context.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetTaxRates), new { id = rate.Id }, rate);
    }

    [HttpPut("tax-rates/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateTaxRate(int id, [FromBody] TaxRate rate, CancellationToken cancellationToken = default)
    {
        if (id != rate.Id) return BadRequest();
        _context.TaxRates.Update(rate);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(rate);
    }

    [HttpDelete("tax-rates/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteTaxRate(int id, CancellationToken cancellationToken = default)
    {
        var rate = await _context.TaxRates.FindAsync(id);
        if (rate == null) return NotFound();
        _context.TaxRates.Remove(rate);
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}

public class ShippingQuoteRequestDto
{
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public string? City { get; set; }
    public string? Zip { get; set; }
}
