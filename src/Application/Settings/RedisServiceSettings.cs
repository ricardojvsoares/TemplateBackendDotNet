using System.ComponentModel.DataAnnotations;

namespace Application.Settings;

public sealed class RedisServiceSettings
{
    [Required]
    public string Host { get; set; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; set; } = 6379;

    public string Password { get; set; } = string.Empty;
}
