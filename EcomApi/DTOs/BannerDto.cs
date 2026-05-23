using System.ComponentModel.DataAnnotations;

namespace EcomApi.DTOs;

public class BannerDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string BgGradient { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public DateTime StartDate { get; set; }
    public int DurationDays { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}

public class CreateBannerDto
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    public string Subtitle { get; set; } = string.Empty;

    [StringLength(200)]
    public string BgGradient { get; set; } = "linear-gradient(135deg, #2874F0, #1a5dc8)";

    [StringLength(50)]
    public string Icon { get; set; } = "devices";

    public DateTime StartDate { get; set; } = DateTime.UtcNow;

    [Range(1, 365)]
    public int DurationDays { get; set; } = 7;

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

public class UpdateBannerDto
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    public string Subtitle { get; set; } = string.Empty;

    [StringLength(200)]
    public string BgGradient { get; set; } = "linear-gradient(135deg, #2874F0, #1a5dc8)";

    [StringLength(50)]
    public string Icon { get; set; } = "devices";

    public DateTime StartDate { get; set; } = DateTime.UtcNow;

    [Range(1, 365)]
    public int DurationDays { get; set; } = 7;

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}
