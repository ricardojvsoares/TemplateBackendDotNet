using Application.Abstractions.Identity;
using Application.Features.Auth;
using ErrorOr;

namespace Application.Features.Auth.Commands.Logout;

public static class LogoutCommandHandler
{
    public static Task<ErrorOr<Success>> HandleAsync(
        LogoutCommand command,
        IUserContext userContext,
        IUserSessionService sessions,
        CancellationToken cancellationToken) =>
        sessions.RevokeCurrentAsync(
            userContext.UserId,
            command.RefreshToken,
            userContext.TokenId,
            userContext.TokenExpiresAt,
            cancellationToken);
}
