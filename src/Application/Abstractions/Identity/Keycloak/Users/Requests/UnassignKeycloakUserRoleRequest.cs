namespace Application.Abstractions.Identity.Keycloak.Users.Requests;

public sealed class UnassignKeycloakUserRoleRequest
{
    public required string UserId { get; init; }
    public required string RoleId { get; init; }
    public required string RoleName { get; init; }
}
