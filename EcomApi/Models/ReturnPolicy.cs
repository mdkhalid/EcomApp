namespace EcomApi.Models;

public class ReturnPolicy
{
    public int Id { get; set; }
    public int ReturnWindowDays { get; set; } = 7;
    public bool IsActive { get; set; } = true;
    public string PolicyText { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? UpdatedBy { get; set; }
}
