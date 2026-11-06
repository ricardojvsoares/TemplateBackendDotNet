using System.Data;
using Application.Abstractions.Data;
using Npgsql;

namespace Persistence.Services.Data;

public sealed class DatabaseService(NpgsqlDataSource dataSource)
    : IDatabaseService, IAsyncDisposable
{
    private NpgsqlConnection? _connection;
    private NpgsqlTransaction? _transaction;
    private bool _disposed;

    public async Task<IDbConnection> CreateConnectionAsync(
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();

        if (_connection is { State: ConnectionState.Open })
        {
            return _connection;
        }

        _connection ??= dataSource.CreateConnection();
        await _connection.OpenAsync(cancellationToken);

        return _connection;
    }

    public async Task<IDbTransaction> BeginTransactionAsync(
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_transaction is not null)
        {
            throw new InvalidOperationException("A transaction is already active on this connection scope.");
        }

        await CreateConnectionAsync(cancellationToken);
        _transaction = await _connection!.BeginTransactionAsync(cancellationToken);
        return _transaction;
    }

    public async Task CommitTransactionAsync(
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_transaction is null)
        {
            throw new InvalidOperationException("No active transaction to commit.");
        }

        NpgsqlTransaction transaction = _transaction;
        try
        {
            await transaction.CommitAsync(cancellationToken);
        }
        finally
        {
            _transaction = null;
            await transaction.DisposeAsync();
        }
    }

    public async Task RollbackTransactionAsync(
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_transaction is null)
        {
            return;
        }

        NpgsqlTransaction transaction = _transaction;
        try
        {
            await transaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            _transaction = null;
            await transaction.DisposeAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        try
        {
            if (_transaction is not null)
            {
                await _transaction.DisposeAsync();
            }
        }
        finally
        {
            _transaction = null;
            if (_connection is not null)
            {
                await _connection.DisposeAsync();
                _connection = null;
            }
        }
    }
}
