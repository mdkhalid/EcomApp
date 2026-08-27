using System.ComponentModel.DataAnnotations;

namespace EcomApi.Models;

public class Setting
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Key { get; set; } = string.Empty;

    public string? Value { get; set; }

    [MaxLength(200)]
    public string? Description { get; set; }

    [MaxLength(50)]
    public string Group { get; set; } = "General";

    public bool IsSensitive { get; set; }
}
