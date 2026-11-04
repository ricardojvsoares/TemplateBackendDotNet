namespace Application.Abstractions.Identity.Keycloak.Users.Requests;

public sealed class SearchKeycloakUsersRequest
{
    public string? Query { get; init; }
    public string? Email { get; init; }
    public int First { get; init; }
    public int Max { get; init; } = 20;
}
