namespace WebApi.RateLimiting;

internal static class RateLimitPolicies
{
    public const string Auth = "auth";

    public const string Upload = "upload";

    public const string Exceeded = "RateLimit.Exceeded";
}
