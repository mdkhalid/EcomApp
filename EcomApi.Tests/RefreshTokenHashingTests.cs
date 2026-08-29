using EcomApi.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace EcomApi.Tests;

public class RefreshTokenHashingTests
{
    private static IConfiguration Config() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:SecretKey"] = "test-secret-key-at-least-32-bytes-long!!!",
            ["Jwt:Issuer"] = "test",
            ["Jwt:Audience"] = "test",
            ["Jwt:AccessTokenExpirationMinutes"] = "15",
            ["Jwt:RefreshTokenExpirationDays"] = "7"
        })
        .Build();

    [Fact]
    public void GenerateRefreshToken_StoresHashOfToken_NotPlainToken()
    {
        var service = new TokenService(Config());
        var token = service.GenerateRefreshToken(1, "127.0.0.1", null);

        Assert.False(string.IsNullOrEmpty(token.Token));
        Assert.Equal(PasswordResetToken.HashToken(token.Token), token.TokenHash);
        Assert.NotEqual(token.Token, token.TokenHash);
    }

    [Fact]
    public void GenerateRefreshToken_HashIsSha256Hex_64Chars()
    {
        var service = new TokenService(Config());
        var token = service.GenerateRefreshToken(1, "127.0.0.1", null);

        Assert.Equal(64, token.TokenHash!.Length);
        Assert.Matches("^[0-9a-fA-F]+$", token.TokenHash);
    }

    [Fact]
    public void GenerateRefreshToken_DifferentTokens_ProduceDifferentHashes()
    {
        var service = new TokenService(Config());
        var a = service.GenerateRefreshToken(1, "127.0.0.1", null);
        var b = service.GenerateRefreshToken(1, "127.0.0.1", null);

        Assert.NotEqual(a.TokenHash, b.TokenHash);
    }
}
