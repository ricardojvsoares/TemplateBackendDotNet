using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace WebApi.Tests;

[Collection(ApiSuite.Name)]
public sealed class RateLimitEndpointTests
{
    private const int TightPermit = 2;

    private const int OpenPermit = 10;

    private const string LoginRoute = "/api/v1/auth/login";

    private const string UsersRoute = "/api/v1/users";

    private const string FilesRoute = "/api/v1/files";

    private static readonly string[] Variables =
    [
        "RATE_LIMIT_PREFIX",
        "RATE_LIMIT_GLOBAL_PERMIT",
        "RATE_LIMIT_GLOBAL_WINDOW_SECONDS",
        "RATE_LIMIT_AUTH_PERMIT",
        "RATE_LIMIT_AUTH_WINDOW_SECONDS",
        "RATE_LIMIT_UPLOAD_PERMIT",
        "RATE_LIMIT_UPLOAD_WINDOW_SECONDS"
    ];

    public RateLimitEndpointTests(ApiFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
    }

    [Fact]
    public async Task ThirdLoginReturnsTooManyRequests()
    {
        await WithClientAsync(OpenPermit, TightPermit, OpenPermit, async client =>
        {
            using HttpResponseMessage first = await PostLoginAsync(client);
            using HttpResponseMessage second = await PostLoginAsync(client);
            using HttpResponseMessage third = await PostLoginAsync(client);

            Assert.Equal(HttpStatusCode.Unauthorized, first.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, second.StatusCode);
            await AssertTooManyRequestsAsync(third);
        });
    }

    [Fact]
    public async Task ThirdAnonymousUserCreateReturnsTooManyRequests()
    {
        await WithClientAsync(TightPermit, OpenPermit, OpenPermit, async client =>
        {
            using HttpResponseMessage first = await PostUserAsync(client);
            using HttpResponseMessage second = await PostUserAsync(client);
            using HttpResponseMessage third = await PostUserAsync(client);

            Assert.Equal(HttpStatusCode.Unauthorized, first.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, second.StatusCode);
            await AssertTooManyRequestsAsync(third);
        });
    }

    [Fact]
    public async Task ThirdUploadReturnsTooManyRequests()
    {
        await WithClientAsync(OpenPermit, OpenPermit, TightPermit, async client =>
        {
            using HttpResponseMessage first = await PostUploadAsync(client);
            using HttpResponseMessage second = await PostUploadAsync(client);
            using HttpResponseMessage third = await PostUploadAsync(client);

            Assert.Equal(HttpStatusCode.BadRequest, first.StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
            await AssertTooManyRequestsAsync(third);
        });
    }

    private static async Task WithClientAsync(
        int globalPermit,
        int authPermit,
        int uploadPermit,
        Func<HttpClient, Task> act)
    {
        Dictionary<string, string?> previous = Capture();
        try
        {
            Apply(globalPermit, authPermit, uploadPermit);
            await using ApiFactory factory = new();
            using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
            await act(client);
        }
        finally
        {
            Restore(previous);
        }
    }

    private static Dictionary<string, string?> Capture()
    {
        var previous = new Dictionary<string, string?>(Variables.Length);
        foreach (string name in Variables)
        {
            previous[name] = Environment.GetEnvironmentVariable(name);
        }

        return previous;
    }

    private static void Apply(int globalPermit, int authPermit, int uploadPermit)
    {
        Environment.SetEnvironmentVariable("RATE_LIMIT_PREFIX", $"probe-{Guid.NewGuid():N}");
        Environment.SetEnvironmentVariable(
            "RATE_LIMIT_GLOBAL_PERMIT",
            globalPermit.ToString(CultureInfo.InvariantCulture));
        Environment.SetEnvironmentVariable("RATE_LIMIT_GLOBAL_WINDOW_SECONDS", "60");
        Environment.SetEnvironmentVariable(
            "RATE_LIMIT_AUTH_PERMIT",
            authPermit.ToString(CultureInfo.InvariantCulture));
        Environment.SetEnvironmentVariable("RATE_LIMIT_AUTH_WINDOW_SECONDS", "60");
        Environment.SetEnvironmentVariable(
            "RATE_LIMIT_UPLOAD_PERMIT",
            uploadPermit.ToString(CultureInfo.InvariantCulture));
        Environment.SetEnvironmentVariable("RATE_LIMIT_UPLOAD_WINDOW_SECONDS", "60");
    }

    private static void Restore(Dictionary<string, string?> previous)
    {
        foreach ((string name, string? value) in previous)
        {
            Environment.SetEnvironmentVariable(name, value);
        }
    }

    private static Task<HttpResponseMessage> PostLoginAsync(HttpClient client) =>
        client.PostAsJsonAsync(
            LoginRoute,
            new { email = "nobody@example.com", password = "not-the-password" });

    private static Task<HttpResponseMessage> PostUserAsync(HttpClient client) =>
        client.PostAsJsonAsync(UsersRoute, new { });

    private static async Task<HttpResponseMessage> PostUploadAsync(HttpClient client)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("one@example.com"), "Recipients");
        return await client.PostAsync(FilesRoute, content);
    }

    private static async Task AssertTooManyRequestsAsync(HttpResponseMessage response)
    {
        string body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.TooManyRequests, body);
        Assert.Contains("RateLimit.Exceeded", body, StringComparison.Ordinal);
        Assert.NotNull(response.Headers.RetryAfter?.Delta);
    }
}
