using System.ComponentModel.DataAnnotations;

namespace Application.Settings;

public sealed class StorageServiceSettings
{
    [Required]
    public required string Endpoint { get; set; }

    [Required]
    public required string AccessKey { get; set; }

    [Required]
    public required string SecretKey { get; set; }

    [Required]
    public required string Bucket { get; set; }

    [Required]
    public required string Region { get; set; }

    public bool ForcePathStyle { get; set; } = true;

    public bool DisablePayloadSigning { get; set; }
}
