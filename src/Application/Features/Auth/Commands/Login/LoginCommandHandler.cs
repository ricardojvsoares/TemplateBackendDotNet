using Application.Abstractions.Identity;
using Application.Features.Auth;
using Domain.Features.Users;
using ErrorOr;

namespace Application.Features.Auth.Commands.Login;

public static class LoginCommandHandler
{
    public static async Task<ErrorOr<AuthTokensResponse>> HandleAsync(
        LoginCommand command,
        IUserRepository users,
        IPasswordHasher passwords,
        IUserSessionService sessions,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Email) || string.IsNullOrEmpty(command.Password))
        {
            return UserErrors.InvalidCredentials();
        }

        string email = UserRules.NormalizeEmail(command.Email);
        ErrorOr<User> found = await users.GetByEmailAsync(email, cancellationToken);
        if (found.IsError && found.FirstError.Type != ErrorType.NotFound)
        {
            return found.Errors;
        }

        User? user = found.IsError ? null : found.Value;
        bool passwordMatches = passwords.Verify(user?.PasswordHash ?? string.Empty, command.Password);
        if (user is null || !user.CanSignIn || !passwordMatches)
        {
            return UserErrors.InvalidCredentials();
        }

        return await sessions.OpenAsync(user, cancellationToken);
    }
}
