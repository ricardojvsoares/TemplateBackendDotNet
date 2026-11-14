using Application.Abstractions.Identity;
using Domain.Authorization;
using Domain.Features.Users;
using ErrorOr;

namespace Application.Features.Users.Commands.CreateUser;

public static class CreateUserCommandHandler
{
    public static async Task<ErrorOr<CreateUserResponse>> HandleAsync(
        CreateUserCommand command,
        IUserContext userContext,
        IUserRepository repository,
        IPasswordHasher passwords,
        CancellationToken cancellationToken)
    {
        ErrorOr<Success> profile = UserRules.ValidateProfile(
            command.FirstName,
            command.LastName,
            command.Email);

        if (profile.IsError)
        {
            return profile.Errors;
        }

        ErrorOr<Success> password = UserRules.ValidatePassword(command.Password);
        if (password.IsError)
        {
            return password.Errors;
        }

        string? globalRole;
        if (!command.HasGlobalRole)
        {
            globalRole = GlobalRoles.Viewer;
        }
        else
        {
            ErrorOr<GlobalRoleSelection> parsedRole = UserRules.ValidateGlobalRole(command.GlobalRole);
            if (parsedRole.IsError)
            {
                return parsedRole.Errors;
            }

            globalRole = parsedRole.Value.Role;
        }

        ErrorOr<string[]> permissions = UserRules.ValidatePermissions(command.Permissions);
        if (permissions.IsError)
        {
            return permissions.Errors;
        }

        string email = UserRules.NormalizeEmail(command.Email);
        ErrorOr<User> existing = await repository.GetByEmailAsync(email, cancellationToken);
        if (!existing.IsError)
        {
            return UserErrors.AlreadyExists(email);
        }

        if (existing.FirstError.Type != ErrorType.NotFound)
        {
            return existing.Errors;
        }

        User user = User.Create(
            command.FirstName.Trim(),
            command.LastName.Trim(),
            email,
            passwords.Hash(command.Password),
            globalRole,
            permissions.Value,
            userContext.UserId);

        ErrorOr<Guid> created = await repository.CreateAsync(user, cancellationToken);
        if (created.IsError)
        {
            return created.Errors;
        }

        return new CreateUserResponse(created.Value);
    }
}
