using Application.Abstractions.Identity;
using Application.Features.Auth;
using Domain.Features.Users;
using ErrorOr;

namespace Application.Features.Auth.Commands.ChangePassword;

public static class ChangePasswordCommandHandler
{
    public static async Task<ErrorOr<Success>> HandleAsync(
        ChangePasswordCommand command,
        IUserContext userContext,
        IUserRepository users,
        IPasswordHasher passwords,
        IUserSessionService sessions,
        CancellationToken cancellationToken)
    {
        ErrorOr<User> found = await users.GetByIdAsync(userContext.UserId, cancellationToken);
        if (found.IsError && found.FirstError.Type != ErrorType.NotFound)
        {
            return found.Errors;
        }

        User? user = found.IsError ? null : found.Value;
        bool passwordMatches = passwords.Verify(user?.PasswordHash ?? string.Empty, command.CurrentPassword ?? string.Empty);
        if (user is null || !user.CanSignIn || !passwordMatches)
        {
            return UserErrors.InvalidCredentials();
        }

        ErrorOr<Success> password = UserRules.ValidatePassword(command.NewPassword);
        if (password.IsError)
        {
            return password.Errors;
        }

        user.ChangePassword(passwords.Hash(command.NewPassword), user.Id);
        ErrorOr<Success> updated = await users.UpdatePasswordAsync(user, cancellationToken);
        if (updated.IsError)
        {
            return updated.Errors;
        }

        return await sessions.RevokeAllAsync(user.Id, cancellationToken);
    }
}
