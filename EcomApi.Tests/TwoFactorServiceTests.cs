using EcomApi.Models;
using EcomApi.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using OtpNet;
using Xunit;

namespace EcomApi.Tests;

public class TwoFactorServiceTests
{
    private static TwoFactorService CreateService()
    {
        var services = new ServiceCollection();
        var keyDir = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "ecom-dp-tests"));
        services.AddDataProtection().PersistKeysToFileSystem(keyDir);
        var provider = services.BuildServiceProvider().GetRequiredService<IDataProtectionProvider>();
        return new TwoFactorService(provider);
    }

    [Fact]
    public void GenerateSetup_ReturnsBase32SecretAndOtpAuthUri()
    {
        var svc = CreateService();
        var (secret, uri) = svc.GenerateSetup("admin@example.com");

        Assert.False(string.IsNullOrWhiteSpace(secret));
        Assert.Contains("otpauth://totp/", uri);
        Assert.Contains("secret=" + secret, uri);
    }

    [Fact]
    public void VerifyCode_AcceptsCurrentTotp_RejectsWrongCode()
    {
        var svc = CreateService();
        var (plain, _) = svc.GenerateSetup("admin@example.com");
        var protectedSecret = svc.ProtectSecret(plain);

        var code = new Totp(Base32Encoding.ToBytes(plain)).ComputeTotp();
        Assert.True(svc.VerifyCode(protectedSecret, code));
        Assert.False(svc.VerifyCode(protectedSecret, "000000"));
    }

    [Fact]
    public void GenerateRecoveryCodes_ProducesUniqueHashedCodes()
    {
        var svc = CreateService();
        var codes = svc.GenerateRecoveryCodes(10);

        Assert.Equal(10, codes.Count);
        Assert.Equal(10, codes.Select(c => c.Plain).Distinct().Count());
        Assert.All(codes, c => Assert.Equal(64, c.Hash.Length));
    }

    [Fact]
    public void TryConsumeRecoveryCode_MarksMatchingCodeUsed()
    {
        var svc = CreateService();
        var user = new User { Email = "a@b.com" };
        var codes = svc.GenerateRecoveryCodes(3);
        foreach (var (_, hash) in codes)
            user.RecoveryCodes.Add(new RecoveryCode { CodeHash = hash });

        var target = codes[1].Plain;
        Assert.True(svc.TryConsumeRecoveryCode(user, target));
        Assert.False(svc.TryConsumeRecoveryCode(user, target)); // already used
        Assert.True(svc.TryConsumeRecoveryCode(user, codes[0].Plain));
    }
}
