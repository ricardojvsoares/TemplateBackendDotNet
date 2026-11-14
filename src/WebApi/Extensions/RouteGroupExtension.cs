using System.Globalization;
using Asp.Versioning;
using Asp.Versioning.Builder;

namespace WebApi.Extensions;

internal static class RouteGroupExtension
{
    /// <summary>
    /// Creates a versioned API route group at <c>api/v{apiVersion}/{prefix}</c>,
    /// applies the corresponding API version set, and adds a tag derived from <paramref name="prefix"/>.
    /// </summary>
    internal static RouteGroupBuilder MapApiGroup(
        this IEndpointRouteBuilder app,
        string prefix,
        int version = 1)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);

        string normalizedPrefix = prefix.Trim().Trim('/');

        ApiVersionSet versionSet = app.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(version))
            .Build();

        string tag = BuildTagFromPrefix(normalizedPrefix);

        return app.MapGroup($"api/v{{apiVersion:apiVersion}}/{prefix}")
            .WithApiVersionSet(versionSet)
            .WithTags(tag)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);
    }

#pragma warning disable CA1308
    private static string BuildTagFromPrefix(string prefix)
    {
        string[] segments = prefix.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        string leaf = segments.Length > 0 ? segments[^1] : prefix;

        string normalized = leaf.Replace('-', ' ').Replace('_', ' ');
        TextInfo textInfo = CultureInfo.InvariantCulture.TextInfo;

        return textInfo.ToTitleCase(normalized.ToLowerInvariant());
    }
#pragma warning restore CA1308
}
