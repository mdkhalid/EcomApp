namespace EcomApi.Models;

public class Banner
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string BgGradient { get; set; } = "linear-gradient(135deg, #2874F0, #1a5dc8)";
    public string Icon { get; set; } = "devices";
    public string? ImageUrl { get; set; }
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public int DurationDays { get; set; } = 7;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
