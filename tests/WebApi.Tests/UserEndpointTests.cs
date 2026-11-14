using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace WebApi.Tests;

[Collection(ApiSuite.Name)]
public sealed class UserEndpointTests
{
    private const string LoginRoute = "/api/v1/auth/login";

    private const string SignupRoute = "/api/v1/auth/signup";

    private const string ChangePasswordRoute = "/api/v1/auth/change-password";

    private const string UsersRoute = "/api/v1/users";

    private const string UserPassword = "UserPass123";

    private const string ReplacementPassword = "NewPass123";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _client;

    public UserEndpointTests(ApiFixture fixture)
    {
        _client = fixture.Client;
    }

    [Fact]
    public async Task ChangePasswordRejectsBadInputThenRevokesTheSession()
    {
        TokenResponse current = await SignupAsync();

        using HttpRequestMessage wrongPassword = Authorized(
            HttpMethod.Post,
            ChangePasswordRoute,
            current.AccessToken,
            new { currentPassword = "not-the-password", newPassword = ReplacementPassword });
        using HttpResponseMessage wrong = await _client.SendAsync(wrongPassword);
        await AssertProblemAsync(wrong, HttpStatusCode.Unauthorized, "User.InvalidCredentials");

        using HttpRequestMessage shortPassword = Authorized(
            HttpMethod.Post,
            ChangePasswordRoute,
            current.AccessToken,
            new { currentPassword = UserPassword, newPassword = "1234567" });
        using HttpResponseMessage tooShort = await _client.SendAsync(shortPassword);
        Assert.Equal(HttpStatusCode.BadRequest, tooShort.StatusCode);

        using HttpRequestMessage change = Authorized(
            HttpMethod.Post,
            ChangePasswordRoute,
            current.AccessToken,
            new { currentPassword = UserPassword, newPassword = ReplacementPassword });
        using HttpResponseMessage changed = await _client.SendAsync(change);
        Assert.Equal(HttpStatusCode.NoContent, changed.StatusCode);

        using HttpRequestMessage reuse = Authorized(HttpMethod.Post, UsersRoute, current.AccessToken, NewUser());
        using HttpResponseMessage denied = await _client.SendAsync(reuse);
        Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);

        using HttpResponseMessage oldLogin = await _client.PostAsJsonAsync(
            LoginRoute,
            new { email = current.Email, password = UserPassword });
        await AssertProblemAsync(oldLogin, HttpStatusCode.Unauthorized, "User.InvalidCredentials");

        using HttpResponseMessage newLogin = await _client.PostAsJsonAsync(
            LoginRoute,
            new { email = current.Email, password = ReplacementPassword });
        Assert.Equal(HttpStatusCode.OK, newLogin.StatusCode);
    }

    [Fact]
    public async Task UpdateUserRejectsBadInputAndRenames()
    {
        TokenResponse admin = await LoginAsync(ApiFixture.AdminEmail, ApiFixture.AdminPassword);

        using HttpRequestMessage blank = Authorized(
            HttpMethod.Put,
            $"{UsersRoute}/{Guid.NewGuid()}",
            admin.AccessToken,
            new { firstName = "", lastName = "Person" });
        using HttpResponseMessage invalid = await _client.SendAsync(blank);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);

        using HttpRequestMessage missing = Authorized(
            HttpMethod.Put,
            $"{UsersRoute}/{Guid.NewGuid()}",
            admin.AccessToken,
            new { firstName = "Renamed", lastName = "Person" });
        using HttpResponseMessage notFound = await _client.SendAsync(missing);
        Assert.Equal(HttpStatusCode.NotFound, notFound.StatusCode);

        CreatedUser created = await CreateUserAsync(admin.AccessToken);
        using HttpRequestMessage rename = Authorized(
            HttpMethod.Put,
            $"{UsersRoute}/{created.Id}",
            admin.AccessToken,
            new { firstName = "Renamed", lastName = "Person" });
        using HttpResponseMessage renamed = await _client.SendAsync(rename);
        Assert.Equal(HttpStatusCode.NoContent, renamed.StatusCode);
    }

    [Fact]
    public async Task SetAccessRejectsBadRoleAndRevokesTheUser()
    {
        TokenResponse admin = await LoginAsync(ApiFixture.AdminEmail, ApiFixture.AdminPassword);

        using HttpRequestMessage badRole = Authorized(
            HttpMethod.Put,
            $"{UsersRoute}/{Guid.NewGuid()}/access",
            admin.AccessToken,
            new { globalRole = "nope", permissions = Array.Empty<string>() });
        using HttpResponseMessage invalid = await _client.SendAsync(badRole);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);

        CreatedUser created = await CreateUserAsync(admin.AccessToken);
        TokenResponse user = await LoginAsync(created.Email, UserPassword);

        using HttpRequestMessage access = Authorized(
            HttpMethod.Put,
            $"{UsersRoute}/{created.Id}/access",
            admin.AccessToken,
            new { globalRole = "viewer", permissions = Array.Empty<string>() });
        using HttpResponseMessage updated = await _client.SendAsync(access);
        Assert.Equal(HttpStatusCode.NoContent, updated.StatusCode);

        using HttpRequestMessage reuse = Authorized(HttpMethod.Post, UsersRoute, user.AccessToken, NewUser());
        using HttpResponseMessage denied = await _client.SendAsync(reuse);
        Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
    }

    [Fact]
    public async Task SetPasswordRejectsShortPasswordsAndReplacesTheSession()
    {
        TokenResponse admin = await LoginAsync(ApiFixture.AdminEmail, ApiFixture.AdminPassword);

        using HttpRequestMessage tooShortRequest = Authorized(
            HttpMethod.Put,
            $"{UsersRoute}/{Guid.NewGuid()}/password",
            admin.AccessToken,
            new { password = "1234567" });
        using HttpResponseMessage tooShort = await _client.SendAsync(tooShortRequest);
        Assert.Equal(HttpStatusCode.BadRequest, tooShort.StatusCode);

        CreatedUser created = await CreateUserAsync(admin.AccessToken);
        TokenResponse user = await LoginAsync(created.Email, UserPassword);

        using HttpRequestMessage reset = Authorized(
            HttpMethod.Put,
            $"{UsersRoute}/{created.Id}/password",
            admin.AccessToken,
            new { password = ReplacementPassword });
        using HttpResponseMessage updated = await _client.SendAsync(reset);
        Assert.Equal(HttpStatusCode.NoContent, updated.StatusCode);

        using HttpResponseMessage oldLogin = await _client.PostAsJsonAsync(
            LoginRoute,
            new { email = created.Email, password = UserPassword });
        await AssertProblemAsync(oldLogin, HttpStatusCode.Unauthorized, "User.InvalidCredentials");

        using HttpResponseMessage newLogin = await _client.PostAsJsonAsync(
            LoginRoute,
            new { email = created.Email, password = ReplacementPassword });
        Assert.Equal(HttpStatusCode.OK, newLogin.StatusCode);

        using HttpRequestMessage reuse = Authorized(HttpMethod.Post, UsersRoute, user.AccessToken, NewUser());
        using HttpResponseMessage denied = await _client.SendAsync(reuse);
        Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
    }

    private async Task<TokenResponse> LoginAsync(string email, string password)
    {
        using HttpResponseMessage response = await _client.PostAsJsonAsync(LoginRoute, new { email, password });
        string body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, body);
        TokenResponse? tokens = JsonSerializer.Deserialize<TokenResponse>(body, JsonOptions);
        Assert.NotNull(tokens);
        return tokens with { Email = email };
    }

    private async Task<TokenResponse> SignupAsync()
    {
        string email = UniqueEmail();
        using HttpResponseMessage response = await _client.PostAsJsonAsync(
            SignupRoute,
            new { firstName = "View", lastName = "User", email, password = UserPassword });
        string body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.Created, body);
        TokenResponse? tokens = JsonSerializer.Deserialize<TokenResponse>(body, JsonOptions);
        Assert.NotNull(tokens);
        return tokens with { Email = email };
    }

    private async Task<CreatedUser> CreateUserAsync(string accessToken)
    {
        string email = UniqueEmail();
        using HttpRequestMessage request = Authorized(
            HttpMethod.Post,
            UsersRoute,
            accessToken,
            new { firstName = "New", lastName = "Person", email, password = UserPassword });
        using HttpResponseMessage response = await _client.SendAsync(request);
        string body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.Created, body);
        CreatedUser? created = JsonSerializer.Deserialize<CreatedUser>(body, JsonOptions);
        Assert.NotNull(created);
        return created with { Email = email };
    }

    private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        string body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == status, body);
        Assert.Contains(code, body, StringComparison.Ordinal);
    }

    private static HttpRequestMessage Authorized(HttpMethod method, string url, string accessToken, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return request;
    }

    private static object NewUser() =>
        new { firstName = "New", lastName = "Person", email = UniqueEmail(), password = UserPassword };

    private static string UniqueEmail() => $"{Guid.NewGuid():N}@example.com";

    private sealed record TokenResponse(string AccessToken, string RefreshToken, DateTime AccessExpiresAt, string Email = "");

    private sealed record CreatedUser(Guid Id, string Email = "");
}
