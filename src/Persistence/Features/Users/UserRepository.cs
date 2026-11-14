using System.Data;
using Application.Abstractions.Data;
using Dapper;
using Domain.Features.Users;
using ErrorOr;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Persistence.Features.Users;

public sealed class UserRepository(
    ILogger<UserRepository> logger,
    IDatabaseService database) : IUserRepository
{
    public Task<ErrorOr<User>> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return Task.FromResult<ErrorOr<User>>(UserErrors.NotFound());
        }

        string normalized = UserRules.NormalizeEmail(email);
        DynamicParameters parameters = new();
        parameters.Add("@p_email", normalized, DbType.String);
        return QueryOneAsync(UserQueries.GetByEmail, parameters, "retrieving the user by email", cancellationToken);
    }

    public Task<ErrorOr<User>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        DynamicParameters parameters = new();
        parameters.Add("@p_id", id, DbType.Guid);
        return QueryOneAsync(UserQueries.GetById, parameters, "retrieving the user", cancellationToken);
    }

    public async Task<ErrorOr<Guid>> CreateAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        try
        {
            IDbConnection connection = await database.CreateConnectionAsync(cancellationToken);
            var command = new CommandDefinition(
                UserQueries.Create,
                CreateParameters(user),
                cancellationToken: cancellationToken);

            Guid id = await connection.ExecuteScalarAsync<Guid>(command);
            return id;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return UserErrors.AlreadyExists(user.Email);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error creating user");
            return Error.Unexpected(description: "An unexpected error occurred while creating the user.");
        }
    }

    public Task<ErrorOr<Success>> UpdateAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        DynamicParameters parameters = new();
        parameters.Add("@p_id", user.Id, DbType.Guid);
        parameters.Add("@p_firstName", user.FirstName, DbType.String);
        parameters.Add("@p_lastName", user.LastName, DbType.String);
        parameters.Add("@p_updatedAt", Timestamps.Normalize(user.UpdatedAt ?? DateTime.UtcNow));
        parameters.Add("@p_updatedBy", user.UpdatedBy, DbType.Guid);
        return ExecuteAsync(UserQueries.Update, parameters, user.Id, "updating the user", cancellationToken);
    }

    public Task<ErrorOr<Success>> UpdatePasswordAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        DynamicParameters parameters = new();
        parameters.Add("@p_id", user.Id, DbType.Guid);
        parameters.Add("@p_passwordHash", user.PasswordHash, DbType.String);
        parameters.Add("@p_updatedAt", Timestamps.Normalize(user.UpdatedAt ?? DateTime.UtcNow));
        parameters.Add("@p_updatedBy", user.UpdatedBy, DbType.Guid);
        return ExecuteAsync(UserQueries.UpdatePassword, parameters, user.Id, "updating the user password", cancellationToken);
    }

    public Task<ErrorOr<Success>> UpdateAccessAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        DynamicParameters parameters = new();
        parameters.Add("@p_id", user.Id, DbType.Guid);
        parameters.Add("@p_globalRole", user.GlobalRole, DbType.String);
        parameters.Add("@p_permissions", user.Permissions);
        parameters.Add("@p_updatedAt", Timestamps.Normalize(user.UpdatedAt ?? DateTime.UtcNow));
        parameters.Add("@p_updatedBy", user.UpdatedBy, DbType.Guid);
        return ExecuteAsync(UserQueries.UpdateAccess, parameters, user.Id, "updating user access", cancellationToken);
    }

    public Task<ErrorOr<Success>> DeleteAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        DateTime deletedAt = user.DeletedAt ?? DateTime.UtcNow;
        DynamicParameters parameters = new();
        parameters.Add("@p_id", user.Id, DbType.Guid);
        parameters.Add("@p_isActive", user.IsActive, DbType.Boolean);
        parameters.Add("@p_deletedAt", Timestamps.Normalize(deletedAt));
        parameters.Add("@p_deletedBy", user.DeletedBy, DbType.Guid);
        parameters.Add("@p_updatedAt", Timestamps.Normalize(user.UpdatedAt ?? deletedAt));
        parameters.Add("@p_updatedBy", user.UpdatedBy, DbType.Guid);
        return ExecuteAsync(UserQueries.Delete, parameters, user.Id, "deleting the user", cancellationToken);
    }

    private async Task<ErrorOr<User>> QueryOneAsync(
        string sql,
        DynamicParameters parameters,
        string operation,
        CancellationToken cancellationToken)
    {
        try
        {
            IDbConnection connection = await database.CreateConnectionAsync(cancellationToken);
            var command = new CommandDefinition(sql, parameters, cancellationToken: cancellationToken);
            User? user = await connection.QuerySingleOrDefaultAsync<User>(command);
            if (user is null)
            {
                return UserErrors.NotFound();
            }

            Normalize(user);
            return user;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error {Operation}", operation);
            return Error.Unexpected(description: $"An unexpected error occurred while {operation}.");
        }
    }

    private async Task<ErrorOr<Success>> ExecuteAsync(
        string sql,
        DynamicParameters parameters,
        Guid userId,
        string operation,
        CancellationToken cancellationToken)
    {
        try
        {
            IDbConnection connection = await database.CreateConnectionAsync(cancellationToken);
            var command = new CommandDefinition(sql, parameters, cancellationToken: cancellationToken);
            int rows = await connection.ExecuteAsync(command);
            if (rows == 0)
            {
                return UserErrors.NotFound(userId);
            }

            return Result.Success;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error {Operation}", operation);
            return Error.Unexpected(description: $"An unexpected error occurred while {operation}.");
        }
    }

    private static void Normalize(User user)
    {
        user.CreatedAt = Timestamps.Normalize(user.CreatedAt);
        user.UpdatedAt = Timestamps.Normalize(user.UpdatedAt);
        user.DeletedAt = Timestamps.Normalize(user.DeletedAt);
    }

    private static DynamicParameters CreateParameters(User user)
    {
        DynamicParameters parameters = new();
        parameters.Add("@p_id", user.Id, DbType.Guid);
        parameters.Add("@p_email", user.Email, DbType.String);
        parameters.Add("@p_firstName", user.FirstName, DbType.String);
        parameters.Add("@p_lastName", user.LastName, DbType.String);
        parameters.Add("@p_passwordHash", user.PasswordHash, DbType.String);
        parameters.Add("@p_globalRole", user.GlobalRole, DbType.String);
        parameters.Add("@p_permissions", user.Permissions);
        parameters.Add("@p_isActive", user.IsActive, DbType.Boolean);
        parameters.Add("@p_createdAt", Timestamps.Normalize(user.CreatedAt));
        parameters.Add("@p_createdBy", user.CreatedBy, DbType.Guid);
        return parameters;
    }
}
