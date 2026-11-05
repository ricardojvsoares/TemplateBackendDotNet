using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using Application.Settings;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services.Identity.Keycloak;

internal sealed class KeycloakAdminTokenProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<KeycloakSettings> options,
    TimeProvider timeProvider) : IDisposable
{
    internal const string HttpClientName = "keycloak-token";

    private static readonly TimeSpan ExpiryMargin = TimeSpan.FromSeconds(30);

    private readonly SemaphoreSlim _lock = new(1, 1);
    private string? _token;
    private DateTimeOffset _expiresAt = DateTimeOffset.MinValue;

    public async Task<string> GetTokenAsync(CancellationToken cancellationToken)
    {
        if (TryGetCached(out string? cached))
        {
            return cached;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (TryGetCached(out cached))
            {
                return cached;
            }

            KeycloakSettings settings = options.Value;

            using HttpClient client = httpClientFactory.CreateClient(HttpClientName);
            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = settings.AdminClientId,
                ["client_secret"] = settings.AdminClientSecret
            });

            using HttpResponseMessage response = await client.PostAsync(
                KeycloakEndpoints.GetTokenUri(settings),
                content,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            KeycloakTokenDto? dto = await response.Content.ReadFromJsonAsync<KeycloakTokenDto>(
                KeycloakJson.Options,
                cancellationToken);

            if (string.IsNullOrEmpty(dto?.AccessToken))
            {
                throw new InvalidOperationException("Keycloak token endpoint returned no access_token.");
            }

            _token = dto.AccessToken;
            _expiresAt = timeProvider.GetUtcNow().AddSeconds(dto.ExpiresIn);

            return _token;
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Dispose() => _lock.Dispose();

    private bool TryGetCached([NotNullWhen(true)] out string? token)
    {
        token = _token;
        return token is not null && timeProvider.GetUtcNow() < _expiresAt - ExpiryMargin;
    }
}