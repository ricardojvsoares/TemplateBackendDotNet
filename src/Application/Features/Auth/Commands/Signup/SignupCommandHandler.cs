using Application.Abstractions.Identity;
using Application.Features.Auth;
using Domain.Authorization;
using Domain.Features.Users;
using ErrorOr;

namespace Application.Features.Auth.Commands.Signup;

public static class SignupCommandHandler
{
    public static async Task<ErrorOr<AuthTokensResponse>> HandleAsync(
        SignupCommand command,
        IUserRepository repository,
        IPasswordHasher passwords,
        IUserSessionService sessions,
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
            GlobalRoles.Viewer,
            [],
            Guid.Empty);

        user.CreatedBy = user.Id;

        ErrorOr<Guid> created = await repository.CreateAsync(user, cancellationToken);
        if (created.IsError)
        {
            return created.Errors;
        }

        return await sessions.OpenAsync(user, cancellationToken);
    }
}
