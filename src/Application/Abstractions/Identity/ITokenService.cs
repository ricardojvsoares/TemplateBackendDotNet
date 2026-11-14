using Domain.Features.Users;

namespace Application.Abstractions.Identity;

public interface ITokenService
{
    IssuedAccessToken CreateAccessToken(User user);

    IssuedRefreshToken CreateRefreshToken();

    string HashRefreshToken(string refreshToken);
}

public sealed record IssuedAccessToken(
    string Token,
    string Jti,
    DateTime ExpiresAt);

public sealed record IssuedRefreshToken(
    string Token,
    string TokenHash,
    DateTime ExpiresAt);
