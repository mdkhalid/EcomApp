namespace EcomApi.Models;

public enum ActivityType
{
    ProductView,
    Search,
    AddToCart,
    Wishlist
}

public class UserActivity
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public User? User { get; set; }
    public string? SessionId { get; set; }
    public ActivityType Type { get; set; }
    public string Data { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
