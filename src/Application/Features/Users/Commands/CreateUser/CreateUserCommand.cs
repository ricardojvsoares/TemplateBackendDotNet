namespace Application.Features.Users.Commands.CreateUser;

public sealed record CreateUserCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    bool HasGlobalRole,
    string? GlobalRole,
    IReadOnlyList<string>? Permissions);
