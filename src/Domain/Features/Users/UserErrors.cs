using ErrorOr;

namespace Domain.Features.Users;

public static class UserErrors
{
    public static Error AlreadyExists(string email) =>
        Error.Conflict(
            code: "User.AlreadyExists",
            description: $"A user with the email '{email}' already exists.");

    public static Error NotFound(Guid id) =>
        Error.NotFound(
            code: "User.NotFound",
            description: $"User '{id}' was not found.");

    public static Error NotFound() =>
        Error.NotFound(
            code: "User.NotFound",
            description: "The user was not found.");

    public static Error InvalidCredentials() =>
        Error.Unauthorized(
            code: "User.InvalidCredentials",
            description: "The email or password is incorrect.");

    public static Error InvalidData() =>
        Error.Validation(
            code: "User.InvalidData",
            description: "First name, last name and email are required.");

    public static Error InvalidPassword() =>
        Error.Validation(
            code: "User.InvalidPassword",
            description: "Password must be between 8 and 128 characters.");

    public static Error InvalidRole(string role) =>
        Error.Validation(
            code: "User.InvalidRole",
            description: $"Global role '{role}' is not valid.");

    public static Error InvalidPermission(string permission) =>
        Error.Validation(
            code: "User.InvalidPermission",
            description: $"Permission '{permission}' is not valid.");
}
