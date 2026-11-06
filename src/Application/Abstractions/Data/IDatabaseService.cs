using System.Data;

namespace Application.Abstractions.Data;

public interface IDatabaseService
{
    Task<IDbConnection> CreateConnectionAsync(
        CancellationToken cancellationToken);

    Task<IDbTransaction> BeginTransactionAsync(
        CancellationToken cancellationToken);

    Task CommitTransactionAsync(
        CancellationToken cancellationToken);

    Task RollbackTransactionAsync(
        CancellationToken cancellationToken);
}
