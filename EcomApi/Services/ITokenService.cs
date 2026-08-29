using EcomApi.Models;

namespace EcomApi.Services;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    RefreshToken GenerateRefreshToken(int userId, string ipAddress, string? userAgent);
    DateTime GetAccessTokenExpiry();
    string GenerateTwoFactorToken(User user);
    int? GetUserIdFromTwoFactorToken(string token);
}
