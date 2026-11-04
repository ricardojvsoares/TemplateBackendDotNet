namespace Application.Abstractions.Identity.Keycloak.Sessions.Requests;

public sealed class RevokeAllUserSessionsRequest
{
    public required string UserId { get; init; }
}
