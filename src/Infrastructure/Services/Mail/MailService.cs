using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using Application.Abstractions.Mail;
using Application.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services.Mail;

public sealed class MailService(
    ILogger<MailService> logger,
    IOptions<MailServiceSettings> options)
    : IMailService
{
    private readonly MailServiceSettings _settings = options.Value;

    public async Task SendAsync(
        IReadOnlyList<string> recipients,
        IReadOnlyList<string>? cc,
        string subject,
        string body,
        IReadOnlyList<EmailAttachment>? attachments,
        CancellationToken cancellationToken)
    {
        if (recipients.Count == 0)
        {
            throw new InvalidOperationException("At least one To recipient is required.");
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_settings.From),
            Subject = subject,
            Body = body,
            IsBodyHtml = false,
        };

        foreach (string address in recipients)
        {
            message.To.Add(address);
        }

        if (cc is not null)
        {
            foreach (string address in cc)
            {
                message.CC.Add(address);
            }
        }

        if (attachments is not null)
        {
            foreach (EmailAttachment attachment in attachments)
            {
                var stream = new MemoryStream(attachment.Content.ToArray(), writable: false);
                message.Attachments.Add(new Attachment(
                    stream,
                    attachment.FileName,
                    string.IsNullOrWhiteSpace(attachment.ContentType)
                        ? MediaTypeNames.Application.Octet
                        : attachment.ContentType));
            }
        }

        using var client = new SmtpClient(_settings.Host, _settings.Port)
        {
            EnableSsl = true,
            DeliveryMethod = SmtpDeliveryMethod.Network,
        };

        if (!string.IsNullOrWhiteSpace(_settings.User))
        {
            client.Credentials = new NetworkCredential(_settings.User, _settings.Password);
        }

        cancellationToken.ThrowIfCancellationRequested();
        await client.SendMailAsync(message, cancellationToken);

        logger.LogInformation(
            "Sent email '{Subject}' to {ToCount} recipient(s) with {AttachmentCount} attachment(s)",
            subject,
            recipients.Count,
            attachments?.Count ?? 0);
    }
}
