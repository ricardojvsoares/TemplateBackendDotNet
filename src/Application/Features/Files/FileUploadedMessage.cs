namespace Application.Features.Files;

public sealed record FileUploadedMessage(
    string FileName,
    string ObjectKey);
