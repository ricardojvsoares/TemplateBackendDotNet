namespace Application.Abstractions.Identity.Keycloak.Sessions.Requests;

public sealed class GetUserSessionsRequest
{
    public required string UserId { get; init; }
}
