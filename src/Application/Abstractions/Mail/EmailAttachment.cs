namespace Application.Abstractions.Mail;

public sealed class EmailAttachment
{
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required ReadOnlyMemory<byte> Content { get; init; }
}
