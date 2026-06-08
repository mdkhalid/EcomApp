namespace EcomApi.Models;

public class PageView
{
    public int Id { get; set; }
    public string Path { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? SessionId { get; set; }
    public int? UserId { get; set; }
    public User? User { get; set; }
    public string? Referrer { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
