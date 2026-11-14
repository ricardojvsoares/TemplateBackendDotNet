using System.Text;
using System.Text.Json;
using Application.Abstractions.Identity;
using Application.Settings;
using Domain.Authorization;
using Domain.Features.Users;
using Infrastructure.Services.Identity;
using Microsoft.Extensions.Options;
using Xunit;

namespace WebApi.Tests;

public sealed class IdentityServiceTests
{
    [Fact]
    public void PasswordHasherAcceptsTheOriginalPasswordOnly()
    {
        var hasher = new PasswordHasher();
        string hash = hasher.Hash("AdminPass123");

        Assert.True(hasher.Verify(hash, "AdminPass123"));
        Assert.False(hasher.Verify(hash, "not-the-password"));
    }

    [Fact]
    public void AccessTokenCarriesTheAdminIdentity()
    {
        var tokens = new TokenService(
            Options.Create(new AuthServiceSettings
            {
                Issuer = "https://tests.local",
                Audience = "tests",
                SigningKey = "test-signing-key-at-least-32-chars",
                AccessTokenMinutes = 15,
                RefreshTokenDays = 7,
                AdminEmail = "admin@example.com",
                AdminPassword = "AdminPass123",
                AdminFirstName = "Ada",
                AdminLastName = "Admin"
            }),
            TimeProvider.System);

        User user = User.Create(
            "Ada",
            "Admin",
            "admin@example.com",
            "hash",
            GlobalRoles.Admin,
            [],
            Guid.Empty);

        IssuedAccessToken issued = tokens.CreateAccessToken(user);
        using JsonDocument document = JsonDocument.Parse(DecodePayload(issued.Token));
        JsonElement root = document.RootElement;

        Assert.Equal(user.Id.ToString(), root.GetProperty("sub").GetString());
        Assert.Equal("admin@example.com", root.GetProperty("email").GetString());
        Assert.Contains("admin", ReadRoles(root.GetProperty("role")));
    }

    private static string DecodePayload(string token)
    {
        string payload = token.Split('.')[1];
        payload = payload.Replace('-', '+').Replace('_', '/');
        payload = (payload.Length % 4) switch
        {
            2 => payload + "==",
            3 => payload + "=",
            _ => payload
        };
        return Encoding.UTF8.GetString(Convert.FromBase64String(payload));
    }

    private static IEnumerable<string?> ReadRoles(JsonElement role)
    {
        if (role.ValueKind == JsonValueKind.Array)
        {
            return role.EnumerateArray().Select(item => item.GetString());
        }

        return [role.GetString()];
    }
}
