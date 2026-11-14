using Application.Abstractions.Identity;
using Application.Features.Auth;
using Domain.Features.Users;
using ErrorOr;

namespace Application.Features.Users.Commands.DeleteUser;

public static class DeleteUserCommandHandler
{
    public static async Task<ErrorOr<Success>> HandleAsync(
        DeleteUserCommand command,
        IUserContext userContext,
        IUserRepository repository,
        IUserSessionService sessions,
        CancellationToken cancellationToken)
    {
        ErrorOr<User> loaded = await repository.GetByIdAsync(command.Id, cancellationToken);
        if (loaded.IsError)
        {
            if (loaded.FirstError.Type == ErrorType.NotFound)
            {
                return UserErrors.NotFound(command.Id);
            }

            return loaded.Errors;
        }

        User user = loaded.Value;
        if (user.IsActive && user.DeletedAt is null)
        {
            user.Delete(userContext.UserId);
            ErrorOr<Success> deleted = await repository.DeleteAsync(user, cancellationToken);
            if (deleted.IsError && deleted.FirstError.Type != ErrorType.NotFound)
            {
                return deleted.Errors;
            }
        }

        return await sessions.RevokeAllAsync(user.Id, cancellationToken);
    }
}
