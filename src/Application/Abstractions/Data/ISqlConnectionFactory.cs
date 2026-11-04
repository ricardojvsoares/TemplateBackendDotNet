using System.Data;

namespace Application.Abstractions.Data;

public interface ISqlConnectionFactory
{
    Task<IDbConnection> CreateConnectionAsync(
        CancellationToken cancellationToken = default);

    Task<IDbTransaction> BeginTransactionAsync(
        CancellationToken cancellationToken = default);

    Task CommitTransactionAsync(
        CancellationToken cancellationToken = default);

    Task RollbackTransactionAsync(
        CancellationToken cancellationToken = default);
}