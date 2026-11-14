using Application.Abstractions.Identity;
using Domain.Features.Users;
using ErrorOr;

namespace Application.Features.Users.Commands.UpdateUser;

public static class UpdateUserCommandHandler
{
    public static async Task<ErrorOr<Success>> HandleAsync(
        UpdateUserCommand command,
        IUserContext userContext,
        IUserRepository repository,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.FirstName) || string.IsNullOrWhiteSpace(command.LastName))
        {
            return UserErrors.InvalidData();
        }

        ErrorOr<User> loaded = UserLookup.RequireManageable(
            await repository.GetByIdAsync(command.Id, cancellationToken),
            command.Id);

        if (loaded.IsError)
        {
            return loaded.Errors;
        }

        User user = loaded.Value;
        user.Update(command.FirstName.Trim(), command.LastName.Trim(), userContext.UserId);

        return await repository.UpdateAsync(user, cancellationToken);
    }
}
