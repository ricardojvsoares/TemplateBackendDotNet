namespace Application.Abstractions.Identity.Keycloak.Users.Requests;

public sealed class UpdateKeycloakUserRequest
{
    public required string Email { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public bool Enabled { get; init; } = true;
}
