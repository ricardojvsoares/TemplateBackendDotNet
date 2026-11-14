using System.ComponentModel.DataAnnotations;
using Domain.Features.Users;

namespace Application.Settings;

public sealed class AuthServiceSettings
{
    public const int MinimumSigningKeyLength = 32;

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    [Required]
    [MinLength(MinimumSigningKeyLength)]
    public string SigningKey { get; set; } = string.Empty;

    [Range(1, 1440)]
    public int AccessTokenMinutes { get; set; } = 15;

    [Range(1, 3650)]
    public int RefreshTokenDays { get; set; } = 7;

    [Required]
    public string AdminEmail { get; set; } = string.Empty;

    [Required]
    [MinLength(UserRules.MinimumPasswordLength)]
    [MaxLength(UserRules.MaximumPasswordLength)]
    public string AdminPassword { get; set; } = string.Empty;

    [Required]
    public string AdminFirstName { get; set; } = string.Empty;

    [Required]
    public string AdminLastName { get; set; } = string.Empty;
}
