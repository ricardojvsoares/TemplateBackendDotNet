using System.Net;
using ErrorOr;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Identity.Keycloak;

internal static class KeycloakErrors
{
    internal static Error UserNotFound(string identifier)
        => Error.NotFound(
            code: "Keycloak.UserNotFound",
            description: $"No Keycloak user found for '{identifier}'.");

    internal static Error ClientNotFound(string clientId)
        => Error.NotFound(
            code: "Keycloak.ClientNotFound",
            description: $"No Keycloak client found with client id '{clientId}'.");

    internal static Error Unexpected(string operation)
        => Error.Unexpected(
            code: "Keycloak.Unexpected",
            description: $"An unexpected error occurred while calling Keycloak ({operation}).");

    internal static async Task<Error> FromResponseAsync(
        HttpResponseMessage response,
        ILogger logger,
        string operation,
        CancellationToken cancellationToken)
    {
        string body = await response.Content.ReadAsStringAsync(cancellationToken);

        logger.LogWarning(
            "Keycloak call '{Operation}' failed with {StatusCode}: {Body}",
            operation,
            (int)response.StatusCode,
            body);

        return response.StatusCode switch
        {
            HttpStatusCode.NotFound => Error.NotFound(
                code: "Keycloak.NotFound",
                description: $"Keycloak resource not found ({operation})."),
            HttpStatusCode.Conflict => Error.Conflict(
                code: "Keycloak.Conflict",
                description: $"Keycloak reported a conflict ({operation}); the resource may already exist."),
            HttpStatusCode.BadRequest => Error.Failure(
                code: "Keycloak.BadRequest",
                description: $"Keycloak rejected the request ({operation})."),
            _ => Unexpected(operation)
        };
    }
}
