namespace Application.Abstractions.Identity.Keycloak.Clients.Requests;

public sealed class GetKeycloakClientRequest
{
    public required string ClientId { get; init; }
    public required bool Search { get; init; } = true;
}
