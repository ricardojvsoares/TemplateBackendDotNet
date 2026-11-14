using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Application.Settings;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using RedisRateLimiting;
using StackExchange.Redis;
using WebApi.RateLimiting;

namespace WebApi.Extensions;

internal static class RateLimitingExtensions
{
    private const string RejectionTitle = "Too many requests.";

    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
    {
        services
            .AddOptions<RateLimitSettings>()
            .Configure<IConfiguration>(Bind)
            .ValidateDataAnnotations()
            .Validate(
                static settings => !string.IsNullOrWhiteSpace(settings.Prefix),
                "RATE_LIMIT_PREFIX must be set.")
            .ValidateOnStart();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = WriteRejectionAsync;
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(GlobalPartition);
            options.AddPolicy(RateLimitPolicies.Auth, AuthPartition);
            options.AddPolicy(RateLimitPolicies.Upload, UploadPartition);
        });

        return services;
    }

    private static void Bind(RateLimitSettings settings, IConfiguration config)
    {
        string? prefix = config["RATE_LIMIT_PREFIX"];
        settings.Prefix = string.IsNullOrWhiteSpace(prefix) ? "rl" : prefix.Trim();
        settings.GlobalPermitLimit = ReadInt(config["RATE_LIMIT_GLOBAL_PERMIT"], settings.GlobalPermitLimit);
        settings.GlobalWindowSeconds = ReadInt(
            config["RATE_LIMIT_GLOBAL_WINDOW_SECONDS"],
            settings.GlobalWindowSeconds);
        settings.AuthPermitLimit = ReadInt(config["RATE_LIMIT_AUTH_PERMIT"], settings.AuthPermitLimit);
        settings.AuthWindowSeconds = ReadInt(config["RATE_LIMIT_AUTH_WINDOW_SECONDS"], settings.AuthWindowSeconds);
        settings.UploadPermitLimit = ReadInt(config["RATE_LIMIT_UPLOAD_PERMIT"], settings.UploadPermitLimit);
        settings.UploadWindowSeconds = ReadInt(
            config["RATE_LIMIT_UPLOAD_WINDOW_SECONDS"],
            settings.UploadWindowSeconds);
    }

    private static int ReadInt(string? value, int fallback) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
            ? parsed
            : fallback;

    private static RateLimitPartition<string> GlobalPartition(HttpContext httpContext)
    {
        RateLimitSettings settings = Settings(httpContext);
        return FixedWindow(
            httpContext,
            $"{settings.Prefix}:global:{GlobalIdentity(httpContext)}",
            settings.GlobalPermitLimit,
            settings.GlobalWindowSeconds);
    }

    private static RateLimitPartition<string> AuthPartition(HttpContext httpContext)
    {
        RateLimitSettings settings = Settings(httpContext);
        return FixedWindow(
            httpContext,
            $"{settings.Prefix}:auth:ip:{RemoteIp(httpContext)}",
            settings.AuthPermitLimit,
            settings.AuthWindowSeconds);
    }

    private static RateLimitPartition<string> UploadPartition(HttpContext httpContext)
    {
        RateLimitSettings settings = Settings(httpContext);
        return FixedWindow(
            httpContext,
            $"{settings.Prefix}:upload:ip:{RemoteIp(httpContext)}",
            settings.UploadPermitLimit,
            settings.UploadWindowSeconds);
    }

    private static RateLimitPartition<string> FixedWindow(
        HttpContext httpContext,
        string partitionKey,
        int permitLimit,
        int windowSeconds)
    {
        IConnectionMultiplexer multiplexer = httpContext.RequestServices
            .GetRequiredService<IConnectionMultiplexer>();
        TimeSpan window = TimeSpan.FromSeconds(windowSeconds);

        return RedisRateLimitPartition.GetFixedWindowRateLimiter(
            partitionKey,
            _ => new RedisFixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = window,
                ConnectionMultiplexerFactory = () => multiplexer
            });
    }

    private static string GlobalIdentity(HttpContext httpContext)
    {
        if (httpContext.User.Identity?.IsAuthenticated == true)
        {
            string? subject = httpContext.User.FindFirstValue("sub");
            if (!string.IsNullOrWhiteSpace(subject))
            {
                return "user:" + subject;
            }
        }

        return "ip:" + RemoteIp(httpContext);
    }

    private static string RemoteIp(HttpContext httpContext)
    {
        string? address = httpContext.Connection.RemoteIpAddress?.ToString();
        return string.IsNullOrWhiteSpace(address) ? "unknown" : address;
    }

    private static RateLimitSettings Settings(HttpContext httpContext) =>
        httpContext.RequestServices.GetRequiredService<IOptions<RateLimitSettings>>().Value;

    private static async ValueTask WriteRejectionAsync(
        OnRejectedContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (context.Lease.TryGetMetadata(RateLimitMetadataName.RetryAfter, out int retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter =
                retryAfter.ToString(CultureInfo.InvariantCulture);
        }

        IResult result = Results.Problem(
            title: RejectionTitle,
            statusCode: StatusCodes.Status429TooManyRequests,
            extensions: new Dictionary<string, object?>
            {
                ["errors"] = new[]
                {
                    new
                    {
                        code = RateLimitPolicies.Exceeded,
                        description = RejectionTitle
                    }
                }
            });

        await result.ExecuteAsync(context.HttpContext);
    }
}
