using System.Net.Http.Json;
using Application.Abstractions.Identity.Keycloak;
using Application.Abstractions.Identity.Keycloak.Clients.Responses;
using Application.Settings;
using ErrorOr;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services.Identity.Keycloak;

internal sealed class KeycloakAdminClientsService(
    HttpClient httpClient,
    IOptions<KeycloakSettings> options,
    ILogger<KeycloakAdminClientsService> logger)
    : IKeycloakAdminClientsService
{
    public async Task<ErrorOr<KeycloakClient>> GetAsync(
        CancellationToken cancellationToken = default)
    {
        string clientId = options.Value.ClientId;

        try
        {
            using HttpResponseMessage response = await httpClient.GetAsync(
                new Uri($"clients?clientId={Uri.EscapeDataString(clientId)}", UriKind.Relative),
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return await KeycloakErrors.FromResponseAsync(response, logger, "get client", cancellationToken);
            }

            List<KeycloakClientDto> clients =
                await response.Content.ReadFromJsonAsync<List<KeycloakClientDto>>(
                    KeycloakJson.Options,
                    cancellationToken) ?? [];

            KeycloakClientDto? dto = clients.Find(
                c => string.Equals(c.ClientId, clientId, StringComparison.Ordinal));

            return dto is null
                ? (ErrorOr<KeycloakClient>)KeycloakErrors.ClientNotFound(clientId)
                : (ErrorOr<KeycloakClient>)new KeycloakClient
                {
                    Id = dto.Id ?? string.Empty,
                    ClientId = dto.ClientId ?? string.Empty,
                    Name = dto.Name ?? string.Empty,
                    Description = dto.Description ?? string.Empty,
                    RootUrl = dto.RootUrl,
                    AdminUrl = dto.AdminUrl,
                    BaseUrl = dto.BaseUrl,
                    Enabled = dto.Enabled,
                    ClientAuthenticatorType = dto.ClientAuthenticatorType ?? string.Empty
                };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting Keycloak client {ClientId}", clientId);
            return KeycloakErrors.Unexpected("get client");
        }
    }
}
