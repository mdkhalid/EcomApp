using System.ComponentModel.DataAnnotations;

namespace EcomApi.DTOs;

public class CouponDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public decimal MinCartValue { get; set; }
    public int MaxUses { get; set; }
    public int CurrentUses { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateCouponDto
{
    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string Code { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public string Type { get; set; } = "Percentage";

    [Required]
    [Range(0.01, 99999.99)]
    public decimal Value { get; set; }

    [Range(0, 99999.99)]
    public decimal MinCartValue { get; set; } = 0;

    [Range(0, int.MaxValue)]
    public int MaxUses { get; set; } = 0;

    [Required]
    public DateTime ExpiresAt { get; set; }

    public bool IsActive { get; set; } = true;
}

public class UpdateCouponDto
{
    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string Code { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public string Type { get; set; } = "Percentage";

    [Required]
    [Range(0.01, 99999.99)]
    public decimal Value { get; set; }

    [Range(0, 99999.99)]
    public decimal MinCartValue { get; set; } = 0;

    [Range(0, int.MaxValue)]
    public int MaxUses { get; set; } = 0;

    [Required]
    public DateTime ExpiresAt { get; set; }

    public bool IsActive { get; set; } = true;
}

public class ValidateCouponRequest
{
    [Required]
    [StringLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    public decimal CartTotal { get; set; }
}

public class ValidateCouponResponse
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Code { get; set; }
    public string? Description { get; set; }
    public string? Type { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalTotal { get; set; }
}
