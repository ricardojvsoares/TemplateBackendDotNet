namespace Application.Features.Users.Commands.SetUserAccess;

public sealed record SetUserAccessCommand(
    Guid Id,
    string? GlobalRole,
    IReadOnlyList<string>? Permissions);
