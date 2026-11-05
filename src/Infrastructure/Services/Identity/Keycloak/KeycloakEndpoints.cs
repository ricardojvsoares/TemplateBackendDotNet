using Application.Settings;

namespace Infrastructure.Services.Identity.Keycloak;

internal static class KeycloakEndpoints
{
    private const string RealmsSegment = "/realms/";
    private const string AdminRealmsSegment = "/admin/realms/";

    internal static Uri GetTokenUri(KeycloakSettings settings)
        => new($"{settings.Authority.TrimEnd('/')}/protocol/openid-connect/token", UriKind.Absolute);

    internal static Uri GetAdminBaseUri(KeycloakSettings settings)
    {
        string authority = settings.Authority.TrimEnd('/');
        int index = authority.IndexOf(RealmsSegment, StringComparison.OrdinalIgnoreCase);

        if (index < 0)
        {
            throw new InvalidOperationException(
                $"Keycloak Authority '{settings.Authority}' must look like 'https://host/realms/<realm>'.");
        }

        string host = authority[..index];
        string realm = authority[(index + RealmsSegment.Length)..];

        return new Uri($"{host}{AdminRealmsSegment}{realm}/", UriKind.Absolute);
    }
}
