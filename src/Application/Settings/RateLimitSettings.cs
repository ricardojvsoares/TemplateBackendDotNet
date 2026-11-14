using System.ComponentModel.DataAnnotations;

namespace Application.Settings;

public sealed class RateLimitSettings
{
    public const int MinimumPermitLimit = 1;

    public const int MaximumPermitLimit = 1_000_000;

    public const int MinimumWindowSeconds = 1;

    public const int MaximumWindowSeconds = 86_400;

    [Required]
    public string Prefix { get; set; } = "rl";

    [Range(MinimumPermitLimit, MaximumPermitLimit)]
    public int GlobalPermitLimit { get; set; } = 300;

    [Range(MinimumWindowSeconds, MaximumWindowSeconds)]
    public int GlobalWindowSeconds { get; set; } = 60;

    [Range(MinimumPermitLimit, MaximumPermitLimit)]
    public int AuthPermitLimit { get; set; } = 10;

    [Range(MinimumWindowSeconds, MaximumWindowSeconds)]
    public int AuthWindowSeconds { get; set; } = 60;

    [Range(MinimumPermitLimit, MaximumPermitLimit)]
    public int UploadPermitLimit { get; set; } = 10;

    [Range(MinimumWindowSeconds, MaximumWindowSeconds)]
    public int UploadWindowSeconds { get; set; } = 60;
}
