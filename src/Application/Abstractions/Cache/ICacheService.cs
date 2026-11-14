namespace Application.Abstractions.Cache;

public interface ICacheService
{
    Task<string?> GetAsync(string key, CancellationToken cancellationToken = default);

    Task SetAsync(string key, string value, TimeSpan ttl, CancellationToken cancellationToken = default);

    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    Task<bool> TryAddAsync(string key, string value, TimeSpan ttl, CancellationToken cancellationToken = default);
}
