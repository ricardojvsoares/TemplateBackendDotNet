namespace Application.Features.Files.Commands.ProcessFile;

public sealed record ProcessFileCommand(
    IReadOnlyList<string> Recipients,
    Stream FileStream,
    string FileName,
    string ContentType
);