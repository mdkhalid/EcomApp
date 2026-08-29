using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using EcomApi.Models;
using Microsoft.IdentityModel.Tokens;

namespace EcomApi.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateAccessToken(User user)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var key = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role),
            new("jti", Guid.NewGuid().ToString())
        };

        if (!string.IsNullOrEmpty(user.FirstName))
            claims.Add(new Claim(ClaimTypes.GivenName, user.FirstName));
        if (!string.IsNullOrEmpty(user.LastName))
            claims.Add(new Claim(ClaimTypes.Surname, user.LastName));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["AccessTokenExpirationMinutes"]!)),
            Issuer = jwtSettings["Issuer"],
            Audience = jwtSettings["Audience"],
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public RefreshToken GenerateRefreshToken(int userId, string ipAddress, string? userAgent)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var refreshToken = new RefreshToken
        {
            UserId = userId,
            Token = token,
            TokenHash = PasswordResetToken.HashToken(token),
            IpAddress = ipAddress,
            UserAgent = userAgent,
            ExpiresAt = DateTime.UtcNow.AddDays(double.Parse(
                _configuration.GetSection("Jwt")["RefreshTokenExpirationDays"]!))
        };

        return refreshToken;
    }

    /// <summary>Validates a 2FA challenge token and returns the embedded user id, or null if invalid/expired.</summary>
    public int? GetUserIdFromTwoFactorToken(string token)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var key = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);
        var handler = new JwtSecurityTokenHandler();

        try
        {
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = "2fa-challenge",
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.Zero
            }, out _);

            var uid = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(uid, out var id) ? id : null;
        }
        catch
        {
            return null;
        }
    }

    public DateTime GetAccessTokenExpiry()
    {
        var minutes = double.Parse(_configuration.GetSection("Jwt")["AccessTokenExpirationMinutes"]!);
        return DateTime.UtcNow.AddMinutes(minutes);
    }

    /// <summary>
    /// Short-lived token (5 min) issued after a correct password when 2FA is
    /// required. It carries the user id but NOT the normal API audience, so it
    /// cannot be used as an access token — only to complete the 2FA challenge.
    /// </summary>
    public string GenerateTwoFactorToken(User user)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var key = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new("2fa_challenge", "true")
        };

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(5),
            Issuer = jwtSettings["Issuer"],
            Audience = "2fa-challenge",
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var handler = new JwtSecurityTokenHandler();
        return handler.WriteToken(handler.CreateToken(descriptor));
    }
}
