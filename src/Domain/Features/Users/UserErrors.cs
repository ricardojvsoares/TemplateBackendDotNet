using ErrorOr;

namespace Domain.Features.Users;

public static class UserErrors
{
    public static Error AlreadyExists(string email)
        => Error.Conflict(
            code: "User.AlreadyExists",
            description: $"A user with the email '{email}' already exists.");

}