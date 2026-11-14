using ErrorOr;

namespace Application.Abstractions.Identity;

public interface IRefreshTokenStore
{
    Task<ErrorOr<Success>> CreateAsync(
        RefreshToken token,
        CancellationToken cancellationToken = default);

    Task<ErrorOr<RefreshToken>> GetByHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task<ErrorOr<Success>> RevokeAsync(
        Guid id,
        DateTime revokedAt,
        CancellationToken cancellationToken = default);

    Task<ErrorOr<IReadOnlyList<RefreshToken>>> RevokeAllForUserAsync(
        Guid userId,
        DateTime revokedAt,
        CancellationToken cancellationToken = default);

    Task<ErrorOr<bool>> TryRevokeAndCreateAsync(
        Guid currentId,
        RefreshToken replacement,
        DateTime revokedAt,
        CancellationToken cancellationToken = default);

    Task<ErrorOr<int>> PurgeExpiredAsync(
        DateTime utcNow,
        CancellationToken cancellationToken = default);
}
