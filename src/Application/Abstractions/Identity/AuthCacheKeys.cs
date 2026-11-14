namespace Application.Abstractions.Identity;

public static class AuthCacheKeys
{
    public static string Deny(string jti) => $"auth:deny:{jti}";
}

public static class AuthTokenItems
{
    public const string ExpiresAt = "auth:access-expires-at";
}
