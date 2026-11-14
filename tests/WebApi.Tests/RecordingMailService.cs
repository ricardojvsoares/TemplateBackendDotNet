using Application.Abstractions.Mail;

namespace WebApi.Tests;

public sealed class RecordingMailService : IMailService
{
    private static readonly Lock Gate = new();

    private static readonly List<RecordedMail> Messages = [];

    public static IReadOnlyList<RecordedMail> Sent
    {
        get
        {
            lock (Gate)
            {
                return [.. Messages];
            }
        }
    }

    public static void Clear()
    {
        lock (Gate)
        {
            Messages.Clear();
        }
    }

    public Task SendAsync(
        IReadOnlyList<string> recipients,
        IReadOnlyList<string>? cc,
        string subject,
        string body,
        IReadOnlyList<EmailAttachment>? attachments,
        CancellationToken cancellationToken)
    {
        var recorded = new RecordedMail(
            recipients.ToArray(),
            subject,
            attachments?.Select(attachment => attachment.FileName).ToArray() ?? []);

        lock (Gate)
        {
            Messages.Add(recorded);
        }

        return Task.CompletedTask;
    }

    public sealed record RecordedMail(
        IReadOnlyList<string> Recipients,
        string Subject,
        IReadOnlyList<string> AttachmentNames);
}
