using System.Security.Claims;
using System.Text.Json;
using Application.Settings;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services.Identity;

internal sealed class KeycloakClaimsTransformation(IOptions<KeycloakSettings> options) : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity { IsAuthenticated: true } identity
            || identity.HasClaim(c => c.Type == ClaimTypes.Role))
        {
            return Task.FromResult(principal);
        }

        var roles = new HashSet<string>(StringComparer.Ordinal);

        AddRoles(identity.FindFirst("realm_access")?.Value, null, roles);
        AddRoles(identity.FindFirst("resource_access")?.Value, options.Value.ClientId, roles);

        foreach (string role in roles)
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, role));
        }

        return Task.FromResult(principal);
    }

    private static void AddRoles(string? json, string? clientKey, HashSet<string> roles)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement node = document.RootElement;

            if (clientKey is not null
                && (node.ValueKind != JsonValueKind.Object || !node.TryGetProperty(clientKey, out node)))
            {
                return;
            }

            if (node.ValueKind != JsonValueKind.Object
                || !node.TryGetProperty("roles", out JsonElement rolesElement)
                || rolesElement.ValueKind != JsonValueKind.Array)
            {
                return;
            }

            foreach (JsonElement item in rolesElement.EnumerateArray())
            {
                if (item.GetString() is { Length: > 0 } role)
                {
                    roles.Add(role);
                }
            }
        }
        catch (JsonException)
        {
            // Malformed claim: ignore, the user simply gets no roles from it.
        }
    }
}
