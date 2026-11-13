using Application.Abstractions.Identity;
using Application.Abstractions.Identity.Keycloak;
using Application.Abstractions.Identity.Keycloak.Users.Requests;
using Domain.Features.Users;
using ErrorOr;
using Microsoft.Extensions.Logging;

namespace Application.Features.Users.Commands.CreateUser;

public static class CreateUserCommandHandler
{
    public static async Task<ErrorOr<CreateUserResponse>> HandleAsync(
        CreateUserCommand command,
        IUserContext userContext,
        IUserRepository repository,
        IKeycloakAdminUsersService keycloakUsers,
        ILogger<CreateUserCommand> logger,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.FirstName)
            || string.IsNullOrWhiteSpace(command.LastName)
            || string.IsNullOrWhiteSpace(command.Email))
        {
            return Error.Validation(
                code: "User.InvalidData",
                description: "First name, last name and email are required.");
        }

        string email = command.Email.Trim();

        ErrorOr<Success> existsByEmail = await repository.ExistsByEmailAsync(email, cancellationToken);

        if (existsByEmail.IsError)
        {
            return existsByEmail.Errors;
        }

        ErrorOr<Created> keycloakUser = await keycloakUsers.CreateAsync(
            new CreateKeycloakUserRequest
            {
                Username = email,
                Email = email,
                FirstName = command.FirstName.Trim(),
                LastName = command.LastName.Trim()
            },
            cancellationToken);

        if (keycloakUser.IsError)
        {
            return keycloakUser.Errors;
        }

        var user = User.Create(
            command.FirstName.Trim(),
            command.LastName.Trim(),
            email,
            userContext.UserId);

        ErrorOr<Guid> createUserResult = await repository.CreateAsync(user, cancellationToken);

        if (createUserResult.IsError)
        {
            await RollbackKeycloakUserAsync(keycloakUsers, email, logger, cancellationToken);

            return createUserResult.Errors;
        }

        ErrorOr<Success> reset = await keycloakUsers.ResetPasswordAsync(
            new ResetKeycloakUserPasswordRequest { UserEmail = email },
            cancellationToken);

        if (reset.IsError)
        {
            logger.LogWarning(
                "User {UserId} was created but the set-password email could not be sent: {Errors}",
                createUserResult.Value,
                reset.Errors);
        }

        return new CreateUserResponse(createUserResult.Value);
    }

    private static async Task RollbackKeycloakUserAsync(
        IKeycloakAdminUsersService keycloakUsers,
        string email,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        ErrorOr<Deleted> rollback = await keycloakUsers.DeleteAsync(
            new DeleteKeycloakUserRequest { UserEmail = email },
            cancellationToken);

        if (rollback.IsError)
        {
            logger.LogError(
                "Could not remove Keycloak user for {Email} after a failed database insert: {Errors}",
                email,
                rollback.Errors);
        }
    }
}
