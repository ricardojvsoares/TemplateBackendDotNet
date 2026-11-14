using Domain.Features.Users;
using ErrorOr;

namespace Application.Features.Auth;

public interface IUserSessionService
{
    Task<ErrorOr<AuthTokensResponse>> OpenAsync(
        User user,
        CancellationToken cancellationToken);

    Task<ErrorOr<AuthTokensResponse>> RotateAsync(
        string refreshToken,
        CancellationToken cancellationToken);

    Task<ErrorOr<Success>> RevokeCurrentAsync(
        Guid userId,
        string? refreshToken,
        string? accessJti,
        DateTime? accessExpiresAt,
        CancellationToken cancellationToken);

    Task<ErrorOr<Success>> RevokeAllAsync(
        Guid userId,
        CancellationToken cancellationToken);
}
