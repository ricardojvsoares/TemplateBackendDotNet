namespace Application.Abstractions.Identity.Keycloak.Sessions.Responses;

public sealed class KeycloakSessionResponse
{
    public required string Id { get; init; }
    public required string UserId { get; init; }
    public required string Username { get; init; }
    public required string IpAddress { get; init; }
    public required DateTime StartedAtUtc { get; init; }
    public required DateTime LastAccessAtUtc { get; init; }
}
