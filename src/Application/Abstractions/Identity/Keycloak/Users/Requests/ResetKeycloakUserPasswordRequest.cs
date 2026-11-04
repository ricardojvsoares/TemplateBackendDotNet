namespace Application.Abstractions.Identity.Keycloak.Users.Requests;

public sealed class ResetKeycloakUserPasswordRequest
{
    public required string UserEmail { get; init; }
}
