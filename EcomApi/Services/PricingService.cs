using EcomApi.Data;
using EcomApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EcomApi.Services;

public record PricingBreakdown(
    decimal Subtotal,
    decimal Discount,
    decimal Shipping,
    decimal Tax,
    decimal Total,
    string? TaxName,
    decimal TaxPercentage);

/// <summary>
/// Server-side source of truth for order totals. Always recomputes shipping and tax
/// from the persisted zone/rate/tax tables so the client can never dictate the amount.
/// </summary>
public class PricingService
{
    private readonly ApplicationDbContext _context;

    public PricingService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PricingBreakdown> ComputeTotalsAsync(
        decimal subtotal, decimal discount, string? city, string? zip, CancellationToken cancellationToken = default)
    {
        var discounted = Math.Max(0, subtotal - discount);
        var shipping = await GetShippingAsync(city, zip, discounted, cancellationToken);
        var (tax, name, pct) = await GetTaxAsync(discounted, city, zip, cancellationToken);
        var total = discounted + shipping + tax;
        return new PricingBreakdown(subtotal, discount, shipping, tax, total, name, pct);
    }

    public async Task<decimal> GetShippingAsync(string? city, string? zip, decimal taxableSubtotal, CancellationToken cancellationToken = default)
    {
        var zones = await _context.ShippingZones
            .Include(z => z.Rates.Where(r => r.IsActive))
            .ToListAsync(cancellationToken);

        var zone = zones.FirstOrDefault(z => ZoneMatches(z.Regions, city, zip))
                   ?? zones.FirstOrDefault(z => z.Regions == "ALL");

        if (zone == null)
            return 0;

        var rate = zone.Rates
            .Where(r => r.MinOrderAmount <= taxableSubtotal)
            .OrderBy(r => r.Rate)
            .FirstOrDefault();

        if (rate == null)
            return 0;

        if (rate.FreeOverAmount.HasValue && taxableSubtotal >= rate.FreeOverAmount.Value)
            return 0;

        return rate.Rate;
    }

    public async Task<(decimal Amount, string? Name, decimal Percentage)> GetTaxAsync(
        decimal taxable, string? city, string? zip, CancellationToken cancellationToken = default)
    {
        var rates = await _context.TaxRates.ToListAsync(cancellationToken);
        var match = rates.FirstOrDefault(t => t.Zone != null && ZoneMatches(t.Zone, city, zip))
                    ?? rates.FirstOrDefault(t => t.IsDefault)
                    ?? rates.FirstOrDefault();

        if (match == null || match.Percentage <= 0)
            return (0, null, 0);

        var amount = Math.Round(taxable * match.Percentage / 100m, 2, MidpointRounding.AwayFromZero);
        return (amount, match.Name, match.Percentage);
    }

    private static bool ZoneMatches(string regions, string? city, string? zip)
    {
        if (string.IsNullOrWhiteSpace(regions) || regions == "ALL")
            return regions == "ALL";

        var tokens = regions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var haystack = $"{city} {zip}".ToLowerInvariant();
        return tokens.Any(t => haystack.Contains(t.ToLowerInvariant()));
    }
}
