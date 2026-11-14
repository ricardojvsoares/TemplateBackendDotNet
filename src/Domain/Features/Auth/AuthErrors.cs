using ErrorOr;

namespace Domain.Features.Auth;

public static class AuthErrors
{
    public static Error InvalidRefreshToken() =>
        Error.Unauthorized(
            code: "Auth.InvalidRefreshToken",
            description: "The refresh token is invalid.");
}
