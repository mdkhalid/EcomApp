using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace EcomApi.Services;

public class EmailService : IEmailService
{
    private readonly ISettingsProvider _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(ISettingsProvider settings, ILogger<EmailService> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var host = await _settings.GetRawAsync("Smtp:Host", cancellationToken);
        var from = await _settings.GetRawAsync("Smtp:From", cancellationToken);
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from))
        {
            _logger.LogWarning("SMTP is not configured (Smtp:Host / Smtp:From). Skipping email to {To}.", to);
            return;
        }

        var port = await _settings.GetAsync("Smtp:Port", 587, cancellationToken);
        var user = await _settings.GetRawAsync("Smtp:User", cancellationToken);
        var pass = await _settings.GetRawAsync("Smtp:Password", cancellationToken);
        var fromName = await _settings.GetRawAsync("Smtp:FromName", cancellationToken) ?? from;
        var enableSsl = await _settings.GetAsync("Smtp:EnableSsl", true, cancellationToken);

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, from));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        try
        {
            using var client = new SmtpClient();
            var secure = enableSsl ? SecureSocketOptions.Auto : SecureSocketOptions.None;
            await client.ConnectAsync(host, port, secure, cancellationToken);
            if (!string.IsNullOrWhiteSpace(user))
            {
                await client.AuthenticateAsync(user, pass, cancellationToken);
            }
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
            _logger.LogInformation("Email sent to {To} with subject '{Subject}'.", to, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To} with subject '{Subject}'.", to, subject);
        }
    }
}
