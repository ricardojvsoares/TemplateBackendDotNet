namespace Application.Abstractions.Identity.Keycloak.Clients.Responses;

public sealed class KeycloakClient
{
    public string Id { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Uri RootUrl { get; set; } = null!;
    public Uri AdminUrl { get; set; } = null!;
    public Uri BaseUrl { get; set; } = null!;
    public bool Enabled { get; set; }
    public string ClientAuthenticatorType { get; set; } = string.Empty;
}
