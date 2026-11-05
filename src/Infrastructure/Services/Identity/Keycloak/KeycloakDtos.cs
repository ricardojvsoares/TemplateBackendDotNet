using System.Text.Json;

namespace Infrastructure.Services.Identity.Keycloak;

internal static class KeycloakJson
{
    internal static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}

internal sealed class KeycloakTokenDto
{
    public string? AccessToken { get; set; }
    public int ExpiresIn { get; set; }
}

internal sealed class KeycloakUserDto
{
    public string? Id { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool Enabled { get; set; }
    public bool EmailVerified { get; set; }
}

internal sealed class KeycloakSessionDto
{
    public string? Id { get; set; }
    public string? UserId { get; set; }
    public string? Username { get; set; }
    public string? IpAddress { get; set; }
    public long Start { get; set; }
    public long LastAccess { get; set; }
}

internal sealed class KeycloakClientDto
{
    public string? Id { get; set; }
    public string? ClientId { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? RootUrl { get; set; }
    public string? AdminUrl { get; set; }
    public string? BaseUrl { get; set; }
    public bool Enabled { get; set; }
    public string? ClientAuthenticatorType { get; set; }
}
