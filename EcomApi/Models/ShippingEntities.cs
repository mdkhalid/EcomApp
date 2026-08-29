namespace EcomApi.Models;

public class ShippingZone
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>Comma-separated match tokens: city, state, or pincode prefixes. Use "ALL" for everywhere.</summary>
    public string Regions { get; set; } = "ALL";
    public List<ShippingRate> Rates { get; set; } = new();
}

public class ShippingRate
{
    public int Id { get; set; }
    public int ShippingZoneId { get; set; }
    public ShippingZone Zone { get; set; } = null!;
    public string Method { get; set; } = "Standard";
    /// <summary>Flat fee for the order (currency units).</summary>
    public decimal Rate { get; set; }
    /// <summary>Orders at/above this amount ship free (null = never free).</summary>
    public decimal? FreeOverAmount { get; set; }
    public decimal MinOrderAmount { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}

public class TaxRate
{
    public int Id { get; set; }
    public string Name { get; set; } = "GST";
    /// <summary>Match token (city/state/pincode prefix) or null for the default rate.</summary>
    public string? Zone { get; set; }
    /// <summary>Percentage, e.g. 18.00 for 18%.</summary>
    public decimal Percentage { get; set; }
    public bool IsDefault { get; set; }
}
