using Application.Abstractions.Identity;
using Application.Features.Auth;
using Domain.Features.Users;
using ErrorOr;

namespace Application.Features.Users.Commands.UpdateUserPassword;

public static class UpdateUserPasswordCommandHandler
{
    public static async Task<ErrorOr<Success>> HandleAsync(
        UpdateUserPasswordCommand command,
        IUserContext userContext,
        IUserRepository repository,
        IPasswordHasher passwords,
        IUserSessionService sessions,
        CancellationToken cancellationToken)
    {
        ErrorOr<Success> password = UserRules.ValidatePassword(command.Password);
        if (password.IsError)
        {
            return password.Errors;
        }

        ErrorOr<User> loaded = UserLookup.RequireManageable(
            await repository.GetByIdAsync(command.Id, cancellationToken),
            command.Id);

        if (loaded.IsError)
        {
            return loaded.Errors;
        }

        User user = loaded.Value;
        user.ChangePassword(passwords.Hash(command.Password), userContext.UserId);

        ErrorOr<Success> updated = await repository.UpdatePasswordAsync(user, cancellationToken);
        if (updated.IsError)
        {
            return updated.Errors;
        }

        return await sessions.RevokeAllAsync(user.Id, cancellationToken);
    }
}
