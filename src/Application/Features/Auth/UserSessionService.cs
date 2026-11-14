using Application.Abstractions.Cache;
using Application.Abstractions.Identity;
using Domain.Features.Auth;
using Domain.Features.Users;
using ErrorOr;
using Microsoft.Extensions.Logging;

namespace Application.Features.Auth;

public sealed class UserSessionService(
    ITokenService tokens,
    IRefreshTokenStore store,
    IUserRepository users,
    ICacheService cache,
    TimeProvider time,
    ILogger<UserSessionService> logger) : IUserSessionService
{
    public async Task<ErrorOr<AuthTokensResponse>> OpenAsync(
        User user,
        CancellationToken cancellationToken)
    {
        IssuedAccessToken access = tokens.CreateAccessToken(user);
        IssuedRefreshToken refresh = tokens.CreateRefreshToken();
        var stored = new RefreshToken
        {
            Id = Guid.CreateVersion7(),
            UserId = user.Id,
            TokenHash = refresh.TokenHash,
            AccessJti = access.Jti,
            AccessExpiresAt = access.ExpiresAt,
            ExpiresAt = refresh.ExpiresAt,
            CreatedAt = time.GetUtcNow().UtcDateTime
        };

        ErrorOr<Success> created = await store.CreateAsync(stored, cancellationToken);
        if (created.IsError)
        {
            return created.Errors;
        }

        return new AuthTokensResponse(access.Token, refresh.Token, access.ExpiresAt);
    }

    public async Task<ErrorOr<AuthTokensResponse>> RotateAsync(
        string refreshToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return AuthErrors.InvalidRefreshToken();
        }

        string hash = tokens.HashRefreshToken(refreshToken);
        ErrorOr<RefreshToken> found = await store.GetByHashAsync(hash, cancellationToken);
        if (found.IsError)
        {
            if (found.FirstError.Type == ErrorType.NotFound)
            {
                return AuthErrors.InvalidRefreshToken();
            }

            return found.Errors;
        }

        RefreshToken current = found.Value;
        if (current.RevokedAt is not null)
        {
            return await RejectReuseAsync(current.UserId, cancellationToken);
        }

        if (AsUtc(current.ExpiresAt) <= time.GetUtcNow().UtcDateTime)
        {
            return AuthErrors.InvalidRefreshToken();
        }

        ErrorOr<User> userResult = await users.GetByIdAsync(current.UserId, cancellationToken);
        if (userResult.IsError)
        {
            if (userResult.FirstError.Type == ErrorType.NotFound)
            {
                return AuthErrors.InvalidRefreshToken();
            }

            return userResult.Errors;
        }

        User user = userResult.Value;
        if (!user.CanSignIn)
        {
            ErrorOr<Success> revoked = await RevokeAllAsync(user.Id, cancellationToken);
            if (revoked.IsError)
            {
                return revoked.Errors;
            }

            return AuthErrors.InvalidRefreshToken();
        }

        IssuedAccessToken access = tokens.CreateAccessToken(user);
        IssuedRefreshToken refresh = tokens.CreateRefreshToken();
        DateTime now = time.GetUtcNow().UtcDateTime;
        var replacement = new RefreshToken
        {
            Id = Guid.CreateVersion7(),
            UserId = user.Id,
            TokenHash = refresh.TokenHash,
            AccessJti = access.Jti,
            AccessExpiresAt = access.ExpiresAt,
            ExpiresAt = refresh.ExpiresAt,
            CreatedAt = now
        };

        ErrorOr<bool> rotated = await store.TryRevokeAndCreateAsync(
            current.Id,
            replacement,
            now,
            cancellationToken);

        if (rotated.IsError)
        {
            return rotated.Errors;
        }

        if (!rotated.Value)
        {
            return await RejectReuseAsync(user.Id, cancellationToken);
        }

        return new AuthTokensResponse(access.Token, refresh.Token, access.ExpiresAt);
    }

    public async Task<ErrorOr<Success>> RevokeCurrentAsync(
        Guid userId,
        string? refreshToken,
        string? accessJti,
        DateTime? accessExpiresAt,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(accessJti) && accessExpiresAt is DateTime currentExpiry)
        {
            await DenylistAsync(accessJti, currentExpiry, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Result.Success;
        }

        string hash = tokens.HashRefreshToken(refreshToken);
        ErrorOr<RefreshToken> found = await store.GetByHashAsync(hash, cancellationToken);
        if (found.IsError)
        {
            if (found.FirstError.Type == ErrorType.NotFound)
            {
                return Result.Success;
            }

            return found.Errors;
        }

        if (found.Value.UserId != userId)
        {
            return Result.Success;
        }

        RefreshToken stored = found.Value;
        if (stored.RevokedAt is null)
        {
            ErrorOr<Success> revoked = await store.RevokeAsync(
                stored.Id,
                time.GetUtcNow().UtcDateTime,
                cancellationToken);

            if (revoked.IsError)
            {
                return revoked.Errors;
            }
        }

        await DenylistAsync(stored.AccessJti, stored.AccessExpiresAt, cancellationToken);
        return Result.Success;
    }

    public async Task<ErrorOr<Success>> RevokeAllAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        DateTime now = time.GetUtcNow().UtcDateTime;
        ErrorOr<IReadOnlyList<RefreshToken>> revoked = await store.RevokeAllForUserAsync(
            userId,
            now,
            cancellationToken);

        if (revoked.IsError)
        {
            return revoked.Errors;
        }

        foreach (RefreshToken token in revoked.Value)
        {
            if (string.IsNullOrWhiteSpace(token.AccessJti))
            {
                continue;
            }

            await DenylistAsync(token.AccessJti, token.AccessExpiresAt, cancellationToken);
        }

        return Result.Success;
    }

    private async Task<ErrorOr<AuthTokensResponse>> RejectReuseAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        logger.LogWarning("Refresh token reuse detected for user {UserId}", userId);
        ErrorOr<Success> revoked = await RevokeAllAsync(userId, cancellationToken);
        if (revoked.IsError)
        {
            return revoked.Errors;
        }

        return AuthErrors.InvalidRefreshToken();
    }

    private async Task DenylistAsync(
        string jti,
        DateTime expiresAt,
        CancellationToken cancellationToken)
    {
        TimeSpan ttl = AsUtc(expiresAt) - time.GetUtcNow().UtcDateTime;
        if (ttl <= TimeSpan.Zero)
        {
            return;
        }

        await cache.SetAsync(AuthCacheKeys.Deny(jti), "1", ttl, cancellationToken);
    }

    private static DateTime AsUtc(DateTime value) =>
        value.Kind == DateTimeKind.Local
            ? value.ToUniversalTime()
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
