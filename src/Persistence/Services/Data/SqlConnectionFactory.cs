using System.Data;
using Application.Abstractions.Data;
using Npgsql;

namespace Persistence.Services.Data;

public class SqlConnectionFactory(NpgsqlDataSource dataSource) : ISqlConnectionFactory, IAsyncDisposable
{
    private NpgsqlConnection? _connection;
    private IDbTransaction? _transaction;

    public async Task<IDbConnection> CreateConnectionAsync(
        CancellationToken cancellationToken = default)
    {
        if (_connection is { State: ConnectionState.Open })
        {
            return _connection;
        }

        _connection = dataSource.CreateConnection();
        await _connection.OpenAsync(cancellationToken);

        return _connection;
    }

    public async Task<IDbTransaction> BeginTransactionAsync(
        CancellationToken cancellationToken = default)
    {
        var connection = await CreateConnectionAsync(cancellationToken);

        if (_transaction is not null)
        {
            throw new InvalidOperationException("A transaction is already active on this connection scope.");
        }

        _transaction = connection.BeginTransaction();
        return _transaction;
    }

    public async Task CommitTransactionAsync(
        CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            throw new InvalidOperationException("No active transaction to commit.");
        }

        try
        {
            _transaction.Commit();
        }
        finally
        {
            _transaction.Dispose();
            _transaction = null;
        }

        await Task.CompletedTask;
    }

    public async Task RollbackTransactionAsync(
        CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            return;
        }

        try
        {
            _transaction.Rollback();
        }
        finally
        {
            _transaction.Dispose();
            _transaction = null;
        }

        await Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        _transaction?.Dispose();
        _transaction = null;

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }

        GC.SuppressFinalize(this);
    }
}