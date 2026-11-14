using System.Data;
using Application.Abstractions.Data;
using Application.Abstractions.Identity;
using Dapper;
using ErrorOr;
using Microsoft.Extensions.Logging;

namespace Persistence.Features.Auth;

public sealed class RefreshTokenStore(
    ILogger<RefreshTokenStore> logger,
    IDatabaseService database) : IRefreshTokenStore
{
    public async Task<ErrorOr<Success>> CreateAsync(
        RefreshToken token,
        CancellationToken cancellationToken = default)
    {
        try
        {
            IDbConnection connection = await database.CreateConnectionAsync(cancellationToken);
            var command = new CommandDefinition(
                RefreshTokenQueries.Create,
                InsertParameters(token),
                cancellationToken: cancellationToken);

            await connection.ExecuteAsync(command);
            return Result.Success;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error creating refresh token");
            return Error.Unexpected(description: "An unexpected error occurred while creating the refresh token.");
        }
    }

    public async Task<ErrorOr<RefreshToken>> GetByHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        try
        {
            IDbConnection connection = await database.CreateConnectionAsync(cancellationToken);
            DynamicParameters parameters = new();
            parameters.Add("@p_tokenHash", tokenHash, DbType.String);
            var command = new CommandDefinition(
                RefreshTokenQueries.GetByHash,
                parameters,
                cancellationToken: cancellationToken);

            RefreshToken? token = await connection.QuerySingleOrDefaultAsync<RefreshToken>(command);
            if (token is null)
            {
                return Error.NotFound(
                    code: "Auth.RefreshTokenNotFound",
                    description: "The refresh token was not found.");
            }

            Normalize(token);
            return token;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error retrieving refresh token");
            return Error.Unexpected(description: "An unexpected error occurred while retrieving the refresh token.");
        }
    }

    public async Task<ErrorOr<Success>> RevokeAsync(
        Guid id,
        DateTime revokedAt,
        CancellationToken cancellationToken = default)
    {
        try
        {
            IDbConnection connection = await database.CreateConnectionAsync(cancellationToken);
            DynamicParameters parameters = new();
            parameters.Add("@p_id", id, DbType.Guid);
            parameters.Add("@p_revokedAt", Timestamps.Normalize(revokedAt));
            var command = new CommandDefinition(
                RefreshTokenQueries.RevokeById,
                parameters,
                cancellationToken: cancellationToken);

            await connection.ExecuteAsync(command);
            return Result.Success;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error revoking refresh token");
            return Error.Unexpected(description: "An unexpected error occurred while revoking the refresh token.");
        }
    }

    public async Task<ErrorOr<IReadOnlyList<RefreshToken>>> RevokeAllForUserAsync(
        Guid userId,
        DateTime revokedAt,
        CancellationToken cancellationToken = default)
    {
        try
        {
            IDbConnection connection = await database.CreateConnectionAsync(cancellationToken);
            DateTime now = Timestamps.Normalize(revokedAt);

            DynamicParameters revokeParameters = new();
            revokeParameters.Add("@p_userId", userId, DbType.Guid);
            revokeParameters.Add("@p_revokedAt", now);
            await connection.ExecuteAsync(new CommandDefinition(
                RefreshTokenQueries.RevokeAllForUser,
                revokeParameters,
                cancellationToken: cancellationToken));

            DynamicParameters listParameters = new();
            listParameters.Add("@p_userId", userId, DbType.Guid);
            listParameters.Add("@p_now", now);
            IEnumerable<RefreshToken> rows = await connection.QueryAsync<RefreshToken>(new CommandDefinition(
                RefreshTokenQueries.ListUnexpiredAccess,
                listParameters,
                cancellationToken: cancellationToken));

            RefreshToken[] tokens = rows.ToArray();
            foreach (RefreshToken token in tokens)
            {
                Normalize(token);
            }

            return tokens;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error revoking user refresh tokens");
            return Error.Unexpected(description: "An unexpected error occurred while revoking the user's sessions.");
        }
    }

    public async Task<ErrorOr<bool>> TryRevokeAndCreateAsync(
        Guid currentId,
        RefreshToken replacement,
        DateTime revokedAt,
        CancellationToken cancellationToken = default)
    {
        bool started = false;
        try
        {
            IDbConnection connection = await database.CreateConnectionAsync(cancellationToken);
            IDbTransaction transaction = await database.BeginTransactionAsync(cancellationToken);
            started = true;

            DynamicParameters revokeParameters = new();
            revokeParameters.Add("@p_id", currentId, DbType.Guid);
            revokeParameters.Add("@p_revokedAt", Timestamps.Normalize(revokedAt));

            int revoked = await connection.ExecuteAsync(new CommandDefinition(
                RefreshTokenQueries.RevokeById,
                revokeParameters,
                transaction,
                cancellationToken: cancellationToken));

            if (revoked == 0)
            {
                await database.RollbackTransactionAsync(CancellationToken.None);
                return false;
            }

            await connection.ExecuteAsync(new CommandDefinition(
                RefreshTokenQueries.Create,
                InsertParameters(replacement),
                transaction,
                cancellationToken: cancellationToken));

            await database.CommitTransactionAsync(cancellationToken);
            return true;
        }
        catch (OperationCanceledException)
        {
            if (started)
            {
                await RollbackQuietlyAsync();
            }

            throw;
        }
        catch (Exception exception)
        {
            if (started)
            {
                await RollbackQuietlyAsync();
            }

            logger.LogError(exception, "Error rotating refresh token");
            return Error.Unexpected(description: "An unexpected error occurred while rotating the refresh token.");
        }
    }

    public async Task<ErrorOr<int>> PurgeExpiredAsync(
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        try
        {
            IDbConnection connection = await database.CreateConnectionAsync(cancellationToken);
            DynamicParameters parameters = new();
            parameters.Add("@p_now", Timestamps.Normalize(utcNow));
            parameters.Add("@p_revokedBefore", Timestamps.Normalize(utcNow.AddDays(-1)));
            var command = new CommandDefinition(
                RefreshTokenQueries.Purge,
                parameters,
                cancellationToken: cancellationToken);

            int deleted = await connection.ExecuteAsync(command);
            return deleted;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error purging refresh tokens");
            return Error.Unexpected(description: "An unexpected error occurred while purging refresh tokens.");
        }
    }

    private async Task RollbackQuietlyAsync()
    {
        try
        {
            await database.RollbackTransactionAsync(CancellationToken.None);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error rolling back refresh token transaction");
        }
    }

    private static void Normalize(RefreshToken token)
    {
        token.AccessExpiresAt = Timestamps.Normalize(token.AccessExpiresAt);
        token.ExpiresAt = Timestamps.Normalize(token.ExpiresAt);
        token.RevokedAt = Timestamps.Normalize(token.RevokedAt);
        token.CreatedAt = Timestamps.Normalize(token.CreatedAt);
    }

    private static DynamicParameters InsertParameters(RefreshToken token)
    {
        DynamicParameters parameters = new();
        parameters.Add("@p_id", token.Id, DbType.Guid);
        parameters.Add("@p_userId", token.UserId, DbType.Guid);
        parameters.Add("@p_tokenHash", token.TokenHash, DbType.String);
        parameters.Add("@p_accessJti", token.AccessJti, DbType.String);
        parameters.Add("@p_accessExpiresAt", Timestamps.Normalize(token.AccessExpiresAt));
        parameters.Add("@p_expiresAt", Timestamps.Normalize(token.ExpiresAt));
        parameters.Add("@p_createdAt", Timestamps.Normalize(token.CreatedAt));
        return parameters;
    }
}
