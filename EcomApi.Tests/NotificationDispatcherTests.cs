using EcomApi.Models;
using EcomApi.Services;
using EcomApi.Services.NotificationChannels;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EcomApi.Tests;

public class NotificationDispatcherTests
{
    [Fact]
    public async Task DispatchAsync_SendsOnlyEnabledChannels()
    {
        var email = new Mock<INotificationChannel>();
        email.SetupGet(c => c.ChannelType).Returns(NotificationChannelType.Email);
        var sms = new Mock<INotificationChannel>();
        sms.SetupGet(c => c.ChannelType).Returns(NotificationChannelType.Sms);

        var settings = new Mock<ISettingsProvider>();
        settings.Setup(s => s.GetAsync("Notification:Email:Enabled", true, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        settings.Setup(s => s.GetAsync("Notification:Sms:Enabled", false, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var dispatcher = new NotificationDispatcher(
            new List<INotificationChannel> { email.Object, sms.Object },
            settings.Object,
            Mock.Of<ILogger<NotificationDispatcher>>());

        var message = new NotificationMessage { Type = NotificationType.Welcome, Email = "a@b.com", Subject = "s", HtmlBody = "b" };
        await dispatcher.DispatchAsync(message);

        email.Verify(c => c.SendAsync(message, It.IsAny<CancellationToken>()), Times.Once);
        sms.Verify(c => c.SendAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DispatchAsync_SkipsDisabledChannel()
    {
        var email = new Mock<INotificationChannel>();
        email.SetupGet(c => c.ChannelType).Returns(NotificationChannelType.Email);

        var settings = new Mock<ISettingsProvider>();
        settings.Setup(s => s.GetAsync("Notification:Email:Enabled", true, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var dispatcher = new NotificationDispatcher(
            new List<INotificationChannel> { email.Object },
            settings.Object,
            Mock.Of<ILogger<NotificationDispatcher>>());

        await dispatcher.DispatchAsync(new NotificationMessage { Type = NotificationType.Welcome, Email = "a@b.com" });

        email.Verify(c => c.SendAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DispatchAsync_ContinuesWhenAChannelThrows()
    {
        var failing = new Mock<INotificationChannel>();
        failing.SetupGet(c => c.ChannelType).Returns(NotificationChannelType.Email);
        failing.Setup(c => c.SendAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        var ok = new Mock<INotificationChannel>();
        ok.SetupGet(c => c.ChannelType).Returns(NotificationChannelType.WhatsApp);

        var settings = new Mock<ISettingsProvider>();
        settings.Setup(s => s.GetAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var dispatcher = new NotificationDispatcher(
            new List<INotificationChannel> { failing.Object, ok.Object },
            settings.Object,
            Mock.Of<ILogger<NotificationDispatcher>>());

        var act = async () => await dispatcher.DispatchAsync(new NotificationMessage { Type = NotificationType.Welcome, Email = "a@b.com" });

        await act(); // must not throw
        ok.Verify(c => c.SendAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
