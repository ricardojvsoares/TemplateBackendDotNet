using Application.Abstractions.Cache;

namespace Application.Tests.Fakes;

internal sealed class FakeCacheService : ICacheService
{
    private readonly Dictionary<string, CacheEntry> _items = [];

    public IReadOnlyDictionary<string, CacheEntry> Items => _items;

    public Task<string?> GetAsync(string key, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.TryGetValue(key, out CacheEntry? entry) ? entry.Value : null);

    public Task SetAsync(
        string key,
        string value,
        TimeSpan ttl,
        CancellationToken cancellationToken = default)
    {
        _items[key] = new CacheEntry(value, ttl);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _items.Remove(key);
        return Task.CompletedTask;
    }

    public Task<bool> TryAddAsync(
        string key,
        string value,
        TimeSpan ttl,
        CancellationToken cancellationToken = default)
    {
        if (_items.ContainsKey(key))
        {
            return Task.FromResult(false);
        }

        _items[key] = new CacheEntry(value, ttl);
        return Task.FromResult(true);
    }

    internal sealed record CacheEntry(string Value, TimeSpan Ttl);
}
