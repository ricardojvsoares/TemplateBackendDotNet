namespace Application.Abstractions.Identity.Keycloak.Sessions.Requests;

public sealed class RevokeUserSessionRequest
{
    public required string SessionId { get; init; }
}
