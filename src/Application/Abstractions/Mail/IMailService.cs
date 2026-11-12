namespace Application.Abstractions.Mail;

public interface IMailService
{
    Task SendAsync(
        IReadOnlyList<string> recipients,
        IReadOnlyList<string>? cc,
        string subject,
        string body,
        IReadOnlyList<EmailAttachment>? attachments,
        CancellationToken cancellationToken);
}
