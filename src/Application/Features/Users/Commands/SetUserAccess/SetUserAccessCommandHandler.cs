using Application.Abstractions.Identity;
using Application.Features.Auth;
using Domain.Features.Users;
using ErrorOr;

namespace Application.Features.Users.Commands.SetUserAccess;

public static class SetUserAccessCommandHandler
{
    public static async Task<ErrorOr<Success>> HandleAsync(
        SetUserAccessCommand command,
        IUserContext userContext,
        IUserRepository repository,
        IUserSessionService sessions,
        CancellationToken cancellationToken)
    {
        ErrorOr<GlobalRoleSelection> globalRole = UserRules.ValidateGlobalRole(command.GlobalRole);
        if (globalRole.IsError)
        {
            return globalRole.Errors;
        }

        ErrorOr<string[]> permissions = UserRules.ValidatePermissions(command.Permissions);
        if (permissions.IsError)
        {
            return permissions.Errors;
        }

        ErrorOr<User> loaded = UserLookup.RequireManageable(
            await repository.GetByIdAsync(command.Id, cancellationToken),
            command.Id);

        if (loaded.IsError)
        {
            return loaded.Errors;
        }

        User user = loaded.Value;
        user.AssignAccess(globalRole.Value.Role, permissions.Value, userContext.UserId);

        ErrorOr<Success> updated = await repository.UpdateAccessAsync(user, cancellationToken);
        if (updated.IsError)
        {
            return updated.Errors;
        }

        return await sessions.RevokeAllAsync(user.Id, cancellationToken);
    }
}
