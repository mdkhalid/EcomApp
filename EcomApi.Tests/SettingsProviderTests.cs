using EcomApi.Models;
using EcomApi.Repositories;
using EcomApi.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace EcomApi.Tests;

public class SettingsProviderTests
{
    [Fact]
    public async Task GetAsync_ReturnsDbValue_WhenPresent()
    {
        var repo = new Mock<ISettingRepository>();
        repo.Setup(r => r.GetByKeyAsync("Smtp:Host", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Setting { Key = "Smtp:Host", Value = "smtp.example.com" });

        var provider = new SettingsProvider(repo.Object, new ConfigurationBuilder().Build(), new MemoryCache(new MemoryCacheOptions()));

        var value = await provider.GetAsync("Smtp:Host", "default", CancellationToken.None);

        Assert.Equal("smtp.example.com", value);
    }

    [Fact]
    public async Task GetAsync_ReturnsDefault_WhenMissing()
    {
        var repo = new Mock<ISettingRepository>();
        repo.Setup(r => r.GetByKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Setting?)null);

        var provider = new SettingsProvider(repo.Object, new ConfigurationBuilder().Build(), new MemoryCache(new MemoryCacheOptions()));

        var value = await provider.GetAsync("Smtp:Port", 587, CancellationToken.None);

        Assert.Equal(587, value);
    }

    [Fact]
    public async Task GetAsync_ConvertsBool_FromString()
    {
        var repo = new Mock<ISettingRepository>();
        repo.Setup(r => r.GetByKeyAsync("Notification:Email:Enabled", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Setting { Key = "Notification:Email:Enabled", Value = "false" });

        var provider = new SettingsProvider(repo.Object, new ConfigurationBuilder().Build(), new MemoryCache(new MemoryCacheOptions()));

        var value = await provider.GetAsync("Notification:Email:Enabled", true, CancellationToken.None);

        Assert.False(value);
    }

    [Fact]
    public async Task GetAsync_CachesAcrossCalls()
    {
        var repo = new Mock<ISettingRepository>();
        repo.Setup(r => r.GetByKeyAsync("Smtp:From", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Setting { Key = "Smtp:From", Value = "noreply@ecom.com" });

        var provider = new SettingsProvider(repo.Object, new ConfigurationBuilder().Build(), new MemoryCache(new MemoryCacheOptions()));

        var first = await provider.GetRawAsync("Smtp:From", CancellationToken.None);
        var second = await provider.GetRawAsync("Smtp:From", CancellationToken.None);

        Assert.Equal("noreply@ecom.com", first);
        Assert.Equal(first, second);
        repo.Verify(r => r.GetByKeyAsync("Smtp:From", It.IsAny<CancellationToken>()), Times.Once);
    }
}
