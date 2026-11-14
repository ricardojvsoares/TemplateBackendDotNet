using Application.Abstractions.Cache;
using StackExchange.Redis;

namespace Infrastructure.Services.Cache;

public sealed class CacheService : ICacheService
{
    private const string KeyPrefix = "cache:";

    private readonly IDatabase _database;

    public CacheService(IConnectionMultiplexer multiplexer)
    {
        _database = multiplexer.GetDatabase();
    }

    public async Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        RedisValue value = await _database.StringGetAsync(Prefix(key)).WaitAsync(cancellationToken);
        return value.IsNull ? null : value.ToString();
    }

    public async Task SetAsync(
        string key,
        string value,
        TimeSpan ttl,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(ttl, TimeSpan.Zero);
        await _database.StringSetAsync(Prefix(key), value, ttl).WaitAsync(cancellationToken);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await _database.KeyDeleteAsync(Prefix(key)).WaitAsync(cancellationToken);
    }

    public async Task<bool> TryAddAsync(
        string key,
        string value,
        TimeSpan ttl,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(ttl, TimeSpan.Zero);
        return await _database
            .StringSetAsync(Prefix(key), value, ttl, When.NotExists)
            .WaitAsync(cancellationToken);
    }

    private static RedisKey Prefix(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return KeyPrefix + key;
    }
}
