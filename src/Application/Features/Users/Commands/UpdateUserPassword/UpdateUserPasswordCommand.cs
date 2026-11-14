namespace Application.Features.Users.Commands.UpdateUserPassword;

public sealed record UpdateUserPasswordCommand(
    Guid Id,
    string Password);
