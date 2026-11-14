using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;
using Application.Abstractions.Mail;
using Application.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services.Mail;

public sealed class MailService(
    ResendHttpClient httpClient,
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

        var payload = new Dictionary<string, object>
        {
            ["from"] = _settings.From,
            ["to"] = recipients,
            ["subject"] = subject,
            ["text"] = body
        };

        if (cc is { Count: > 0 })
        {
            payload["cc"] = cc;
        }

        if (attachments is { Count: > 0 })
        {
            payload["attachments"] = attachments.Select(ToAttachment).ToArray();
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "emails")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);

        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            int status = (int)response.StatusCode;
            logger.LogError(
                "Resend rejected email '{Subject}' with status {StatusCode}",
                subject,
                status);
            throw new InvalidOperationException($"Resend rejected the email with status {status}.");
        }

        logger.LogInformation(
            "Sent email '{Subject}' to {ToCount} recipient(s) with {AttachmentCount} attachment(s)",
            subject,
            recipients.Count,
            attachments?.Count ?? 0);
    }

    private static Dictionary<string, string> ToAttachment(EmailAttachment attachment)
    {
        return new Dictionary<string, string>
        {
            ["filename"] = attachment.FileName,
            ["content"] = Convert.ToBase64String(attachment.Content.Span),
            ["content_type"] = string.IsNullOrWhiteSpace(attachment.ContentType)
                ? MediaTypeNames.Application.Octet
                : attachment.ContentType
        };
    }
}
