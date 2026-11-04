namespace Application.Settings;

public sealed class StorageSettings
{
    public required string Endpoint { get; set; }
    public required int AccessKey { get; set; }
    public required string SecretKey { get; set; }
    public required string Bucket { get; set; }
    public required string Region { get; set; }
}