using Application.Features.Auth;
using ErrorOr;

namespace Application.Features.Auth.Commands.Refresh;

public static class RefreshTokenCommandHandler
{
    public static Task<ErrorOr<AuthTokensResponse>> HandleAsync(
        RefreshTokenCommand command,
        IUserSessionService sessions,
        CancellationToken cancellationToken) =>
        sessions.RotateAsync(command.RefreshToken, cancellationToken);
}
