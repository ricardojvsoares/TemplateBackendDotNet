using Application.Abstractions.Mail;

namespace Application.Tests.Fakes;

internal sealed class FakeMailService : IMailService
{
    private readonly List<SentMail> _sent = [];

    public IReadOnlyList<SentMail> Sent => _sent;

    public Task SendAsync(
        IReadOnlyList<string> recipients,
        IReadOnlyList<string>? cc,
        string subject,
        string body,
        IReadOnlyList<EmailAttachment>? attachments,
        CancellationToken cancellationToken)
    {
        _sent.Add(new SentMail(recipients, cc, subject, body, attachments));
        return Task.CompletedTask;
    }

    internal sealed record SentMail(
        IReadOnlyList<string> Recipients,
        IReadOnlyList<string>? Cc,
        string Subject,
        string Body,
        IReadOnlyList<EmailAttachment>? Attachments);
}
