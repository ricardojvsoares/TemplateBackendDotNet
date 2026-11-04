namespace Application.Abstractions.Identity.Keycloak.Users.Requests;

public sealed class DeleteKeycloakUserRequest
{
    public required string UserEmail { get; init; }
}
