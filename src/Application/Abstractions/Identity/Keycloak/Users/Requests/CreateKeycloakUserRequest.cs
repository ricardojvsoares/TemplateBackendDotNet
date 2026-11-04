namespace Application.Abstractions.Identity.Keycloak.Users.Requests;

public sealed class CreateKeycloakUserRequest
{
    public required string Username { get; init; }
    public required string Email { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public bool Enabled { get; init; } = true;
    public bool EmailVerified { get; init; } = true;
}
