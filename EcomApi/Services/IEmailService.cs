using EcomApi.Models;

namespace EcomApi.Services;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string htmlBody, IReadOnlyList<EmailAttachment>? attachments = null, CancellationToken cancellationToken = default);
}
