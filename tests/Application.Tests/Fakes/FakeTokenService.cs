using Application.Abstractions.Identity;
using Domain.Features.Users;

namespace Application.Tests.Fakes;

internal sealed class FakeTokenService : ITokenService
{
    private int _accessCount;

    private int _refreshCount;

    public DateTime AccessExpiresAt { get; set; } = new(2026, 1, 2, 4, 4, 5, DateTimeKind.Utc);

    public DateTime RefreshExpiresAt { get; set; } = new(2026, 1, 9, 3, 4, 5, DateTimeKind.Utc);

    public IssuedAccessToken CreateAccessToken(User user)
    {
        _accessCount++;
        return new IssuedAccessToken($"access-{_accessCount}", $"jti-{_accessCount}", AccessExpiresAt);
    }

    public IssuedRefreshToken CreateRefreshToken()
    {
        _refreshCount++;
        string token = $"refresh-{_refreshCount}";
        return new IssuedRefreshToken(token, HashRefreshToken(token), RefreshExpiresAt);
    }

    public string HashRefreshToken(string refreshToken) => "hash:" + refreshToken;
}
