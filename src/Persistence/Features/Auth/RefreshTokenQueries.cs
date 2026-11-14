namespace Persistence.Features.Auth;

internal static class RefreshTokenQueries
{
    public const string Columns = @"
        id,
        user_id,
        token_hash,
        access_jti,
        access_expires_at,
        expires_at,
        revoked_at,
        created_at";

    public const string Create = @"
        INSERT INTO refresh_tokens
            (id, user_id, token_hash, access_jti, access_expires_at, expires_at, created_at)
        VALUES
            (@p_id, @p_userId, @p_tokenHash, @p_accessJti, @p_accessExpiresAt, @p_expiresAt, @p_createdAt)";

    public const string GetByHash = $@"
        SELECT {Columns}
        FROM refresh_tokens
        WHERE token_hash = @p_tokenHash
        LIMIT 1";

    public const string RevokeById = @"
        UPDATE refresh_tokens
        SET revoked_at = @p_revokedAt
        WHERE id = @p_id
          AND revoked_at IS NULL";

    public const string RevokeAllForUser = @"
        UPDATE refresh_tokens
        SET revoked_at = @p_revokedAt
        WHERE user_id = @p_userId
          AND revoked_at IS NULL";

    public const string ListUnexpiredAccess = $@"
        SELECT {Columns}
        FROM refresh_tokens
        WHERE user_id = @p_userId
          AND access_expires_at > @p_now";

    public const string Purge = @"
        DELETE FROM refresh_tokens
        WHERE expires_at < @p_now
           OR (revoked_at IS NOT NULL AND revoked_at < @p_revokedBefore)";
}
