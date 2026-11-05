namespace Application.Settings;

public sealed class KeycloakSettings
{
    public required string Authority { get; set; }
    public required string Issuer { get; set; }
    public required string Audience { get; set; }
    public required string ClientId { get; set; }
    public required string MetadataAddress { get; set; }
    public bool RequireHttpsMetadata { get; set; } = true;
    public required string AdminClientId { get; set; }
    public required string AdminClientSecret { get; set; }
}