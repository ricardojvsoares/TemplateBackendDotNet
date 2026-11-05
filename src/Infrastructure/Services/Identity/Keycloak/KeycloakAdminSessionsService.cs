using System.Net.Http.Json;
using Application.Abstractions.Identity.Keycloak;
using Application.Abstractions.Identity.Keycloak.Sessions.Requests;
using Application.Abstractions.Identity.Keycloak.Sessions.Responses;
using ErrorOr;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Identity.Keycloak;

internal sealed class KeycloakAdminSessionsService(
    HttpClient httpClient,
    ILogger<KeycloakAdminSessionsService> logger)
    : IKeycloakAdminSessionsService
{
    public async Task<ErrorOr<IReadOnlyCollection<KeycloakSessionResponse>>> GetUserSessionsAsync(
        GetUserSessionsRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using HttpResponseMessage response = await httpClient.GetAsync(
                new Uri($"users/{Uri.EscapeDataString(request.UserId)}/sessions", UriKind.Relative),
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return await KeycloakErrors.FromResponseAsync(response, logger, "get user sessions", cancellationToken);
            }

            List<KeycloakSessionDto> sessions =
                await response.Content.ReadFromJsonAsync<List<KeycloakSessionDto>>(
                    KeycloakJson.Options,
                    cancellationToken) ?? [];

            return sessions.ConvertAll(s => new KeycloakSessionResponse
            {
                Id = s.Id ?? string.Empty,
                UserId = s.UserId ?? request.UserId,
                Username = s.Username ?? string.Empty,
                IpAddress = s.IpAddress ?? string.Empty,
                StartedAtUtc = DateTimeOffset.FromUnixTimeMilliseconds(s.Start).UtcDateTime,
                LastAccessAtUtc = DateTimeOffset.FromUnixTimeMilliseconds(s.LastAccess).UtcDateTime
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting Keycloak sessions for user {UserId}", request.UserId);
            return KeycloakErrors.Unexpected("get user sessions");
        }
    }

    public async Task<ErrorOr<Success>> RevokeSessionAsync(
        RevokeUserSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using HttpResponseMessage response = await httpClient.DeleteAsync(
                new Uri($"sessions/{Uri.EscapeDataString(request.SessionId)}", UriKind.Relative),
                cancellationToken);

            return !response.IsSuccessStatusCode
                ? (ErrorOr<Success>)await KeycloakErrors.FromResponseAsync(response, logger, "revoke session", cancellationToken)
                : (ErrorOr<Success>)Result.Success;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error revoking Keycloak session {SessionId}", request.SessionId);
            return KeycloakErrors.Unexpected("revoke session");
        }
    }

    public async Task<ErrorOr<Success>> RevokeAllUserSessionsAsync(
        RevokeAllUserSessionsRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using HttpResponseMessage response = await httpClient.PostAsync(
                new Uri($"users/{Uri.EscapeDataString(request.UserId)}/logout", UriKind.Relative),
                content: null,
                cancellationToken);

            return !response.IsSuccessStatusCode
                ? (ErrorOr<Success>)await KeycloakErrors.FromResponseAsync(response, logger, "revoke all sessions", cancellationToken)
                : (ErrorOr<Success>)Result.Success;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error revoking all Keycloak sessions for user {UserId}", request.UserId);
            return KeycloakErrors.Unexpected("revoke all sessions");
        }
    }
}
