using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Application.Abstractions.Identity;
using Application.Settings;
using Domain.Features.Users;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Services.Identity;

public sealed class TokenService(
    IOptions<AuthServiceSettings> options,
    TimeProvider time) : ITokenService
{
    private readonly AuthServiceSettings _settings = options.Value;

    public IssuedAccessToken CreateAccessToken(User user)
    {
        DateTime now = time.GetUtcNow().UtcDateTime;
        DateTime expiresAt = now.AddMinutes(_settings.AccessTokenMinutes);
        string jti = Guid.CreateVersion7().ToString();

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, jti)
        };

        if (user.GlobalRole is not null)
        {
            claims.Add(new Claim(AuthClaims.Role, user.GlobalRole));
        }

        foreach (string permission in user.EffectivePermissions())
        {
            claims.Add(new Claim(AuthClaims.Role, permission));
        }

        var handler = new JwtSecurityTokenHandler();
        handler.OutboundClaimTypeMap.Clear();

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: now,
            expires: expiresAt,
            signingCredentials: new SigningCredentials(SigningKey(), SecurityAlgorithms.HmacSha256));

        return new IssuedAccessToken(handler.WriteToken(token), jti, expiresAt);
    }

    public IssuedRefreshToken CreateRefreshToken()
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(32);
        string token = Base64UrlEncoder.Encode(bytes);
        DateTime expiresAt = time.GetUtcNow().UtcDateTime.AddDays(_settings.RefreshTokenDays);
        return new IssuedRefreshToken(token, HashRefreshToken(token), expiresAt);
    }

    public string HashRefreshToken(string refreshToken)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToHexString(hash);
    }

    private SymmetricSecurityKey SigningKey() =>
        new(Encoding.UTF8.GetBytes(_settings.SigningKey));
}
