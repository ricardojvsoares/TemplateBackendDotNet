using Application.Abstractions.Identity;
using Domain.Features.Users;
using ErrorOr;

namespace Application.Features.Users.Commands.CreateUser;

public static class CreateUserCommandHandler
{
    public static async Task<ErrorOr<CreateUserResponse>> HandleAsync(
        CreateUserCommand command,
        IUserContext userContext,
        IUserRepository repository,
        CancellationToken cancellationToken)
    {
        var existsByEmail = await repository.ExistsByEmailAsync(command.Email, cancellationToken);

        if (existsByEmail.IsError)
        {
            return existsByEmail.Errors;
        }

        var createdBy = userContext.UserId;

        var user = User.Create(
            command.FirstName,
            command.LastName,
            command.Email,
            createdBy);

        var createUserResult = await repository.CreateAsync(user, cancellationToken);

        if (createUserResult.IsError)
        {
            return createUserResult.Errors;
        }

        var id = new CreateUserResponse(createUserResult.Value);

        return id;
    }

}