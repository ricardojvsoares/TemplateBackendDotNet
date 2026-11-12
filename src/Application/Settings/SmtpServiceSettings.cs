using System.ComponentModel.DataAnnotations;

namespace Application.Settings;

public sealed class MailServiceSettings
{
    [Required]
    public string Host { get; set; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; set; } = 587;

    public bool Secure { get; set; }

    public string User { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    [Required]
    public string From { get; set; } = string.Empty;
}
