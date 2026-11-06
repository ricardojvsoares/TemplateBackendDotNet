using System.ComponentModel.DataAnnotations;

namespace Application.Settings;

public sealed class DatabaseServiceSettings
{

    [Required]
    public required string Host { get; set; }

    [Required]
    public int Port { get; set; }

    [Required]
    public required string Database { get; set; }

    [Required]
    public required string Username { get; set; }

    [Required]
    public required string Password { get; set; }
}