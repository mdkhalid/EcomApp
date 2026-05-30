namespace EcomApi.Models;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "Customer";
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public string? CreatedBy { get; set; }

    public List<Cart> Carts { get; set; } = new();
    public List<Order> Orders { get; set; } = new();
    public List<RefreshToken> RefreshTokens { get; set; } = new();
    public List<Address> Addresses { get; set; } = new();
}

public class RefreshToken
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string Token { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByToken { get; set; }
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive => RevokedAt == null && !IsExpired;
}

public static class UserRoles
{
    public const string Admin = "Admin";
    public const string SubAdmin = "SubAdmin";
    public const string Customer = "Customer";

    public static List<string> AllRoles => new List<string> { Admin, SubAdmin, Customer };

    public static bool IsValidRole(string role)
    {
        return AllRoles.Contains(role);
    }
}
