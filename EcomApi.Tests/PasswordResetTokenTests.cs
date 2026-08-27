using EcomApi.Services;
using Xunit;

namespace EcomApi.Tests;

public class PasswordResetTokenTests
{
    [Fact]
    public void GenerateToken_Returns64HexChars()
    {
        var token = PasswordResetToken.GenerateToken();
        Assert.Equal(64, token.Length);
        Assert.Matches("^[0-9a-fA-F]+$", token);
    }

    [Fact]
    public void HashToken_IsDeterministic()
    {
        var token = PasswordResetToken.GenerateToken();
        Assert.Equal(PasswordResetToken.HashToken(token), PasswordResetToken.HashToken(token));
    }

    [Fact]
    public void IsValid_TrueForMatchingToken()
    {
        var token = PasswordResetToken.GenerateToken();
        var hash = PasswordResetToken.HashToken(token);
        Assert.True(PasswordResetToken.IsValid(token, hash, DateTime.UtcNow.AddMinutes(10)));
    }

    [Fact]
    public void IsValid_FalseForWrongToken()
    {
        var token = PasswordResetToken.GenerateToken();
        var hash = PasswordResetToken.HashToken(token);
        Assert.False(PasswordResetToken.IsValid("deadbeef", hash, DateTime.UtcNow.AddMinutes(10)));
    }

    [Fact]
    public void IsValid_FalseWhenExpired()
    {
        var token = PasswordResetToken.GenerateToken();
        var hash = PasswordResetToken.HashToken(token);
        Assert.False(PasswordResetToken.IsValid(token, hash, DateTime.UtcNow.AddMinutes(-1)));
    }

    [Fact]
    public void IsValid_FalseWhenNull()
    {
        Assert.False(PasswordResetToken.IsValid(null, null, null));
    }
}
