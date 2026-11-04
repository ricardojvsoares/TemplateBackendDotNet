using System.Data;
using Application.Abstractions.Data;
using Dapper;
using Domain.Features.Users;
using ErrorOr;
using Microsoft.Extensions.Logging;

namespace Persistence.Features.Users;

internal sealed class UserRepository(
    ILogger<UserRepository> logger,
    ISqlConnectionFactory sqlConnectionFactory)
    : IUserRepository
{
    private readonly ISqlConnectionFactory _sqlConnectionFactory = sqlConnectionFactory;

    public async Task<ErrorOr<Success>> ExistsByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await _sqlConnectionFactory.CreateConnectionAsync(cancellationToken);

            DynamicParameters parameters = new();
            parameters.Add("@p_email", email, DbType.String);

            var command = new CommandDefinition(
                UserQueries.ExistsByEmail,
                parameters,
                transaction: connection.BeginTransaction(),
                cancellationToken: cancellationToken);

            var exists = await connection.ExecuteScalarAsync<int?>(command);

            return exists.HasValue
                ? UserErrors.AlreadyExists(email)
                : (ErrorOr<Success>)Result.Success;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking user by email");
            return Error.Unexpected(description: "An unexpected error occurred while retrieving the user by email.");
        }
    }

    public async Task<ErrorOr<Guid>> CreateAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await _sqlConnectionFactory.CreateConnectionAsync(cancellationToken);

            DynamicParameters parameters = new();
            parameters.Add("@p_id", user.Id, DbType.Guid);
            parameters.Add("@p_firstName", user.FirstName, DbType.String);
            parameters.Add("@p_lastName", user.LastName, DbType.String);
            parameters.Add("@p_email", user.Email, DbType.String);
            parameters.Add("@p_createdAt", user.CreatedAt, DbType.DateTime);
            parameters.Add("@p_createdBy", user.CreatedBy, DbType.Guid);

            var command = new CommandDefinition(
                UserQueries.Create,
                parameters,
                transaction: connection.BeginTransaction(),
                cancellationToken: cancellationToken);

            var id = await connection.ExecuteScalarAsync<Guid>(command);

            return id;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating user");
            return Error.Unexpected(description: "An unexpected error occurred while creating the user.");
        }
    }

}