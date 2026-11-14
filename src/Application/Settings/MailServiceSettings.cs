using System.ComponentModel.DataAnnotations;

namespace Application.Settings;

public sealed class MailServiceSettings
{
    [Required]
    public string ApiKey { get; set; } = string.Empty;

    [Required]
    public string From { get; set; } = string.Empty;
}
