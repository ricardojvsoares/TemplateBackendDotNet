using System.Security.Claims;
using Application.Abstractions.Identity;

namespace Application.Tests.Fakes;

internal sealed class FakeUserContext : IUserContext
{
    public bool IsAuthenticated { get; set; } = true;

    public string UserEmail { get; set; } = "actor@example.com";

    public Guid UserId { get; set; } = Guid.CreateVersion7();

    public string? TokenId { get; set; }

    public DateTime? TokenExpiresAt { get; set; }

    public string? Role { get; set; }

    public IEnumerable<Claim> Roles { get; set; } = [];

    public string? LanguageCode { get; set; }

    public long LanguageId { get; set; }
}
