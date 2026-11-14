using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace WebApi.Tests;

[Collection(ApiSuite.Name)]
public sealed class AuthEndpointTests
{
    private const string LoginRoute = "/api/v1/auth/login";

    private const string RefreshRoute = "/api/v1/auth/refresh";

    private const string LogoutRoute = "/api/v1/auth/logout";

    private const string SignupRoute = "/api/v1/auth/signup";

    private const string UsersRoute = "/api/v1/users";

    private const string UserPassword = "UserPass123";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _client;

    public AuthEndpointTests(ApiFixture fixture)
    {
        _client = fixture.Client;
    }

    [Fact]
    public async Task SeededAdminLoginReturnsTokens()
    {
        TokenResponse tokens = await LoginAsync(ApiFixture.AdminEmail, ApiFixture.AdminPassword);

        Assert.False(string.IsNullOrWhiteSpace(tokens.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(tokens.RefreshToken));
        Assert.NotEqual(default, tokens.AccessExpiresAt);
    }

    [Fact]
    public async Task WrongPasswordAndUnknownEmailAreRejected()
    {
        using HttpResponseMessage wrongPassword = await _client.PostAsJsonAsync(
            LoginRoute,
            new { email = ApiFixture.AdminEmail, password = "not-the-password" });
        await AssertProblemAsync(wrongPassword, HttpStatusCode.Unauthorized, "User.InvalidCredentials");

        using HttpResponseMessage unknownEmail = await _client.PostAsJsonAsync(
            LoginRoute,
            new { email = UniqueEmail(), password = ApiFixture.AdminPassword });
        await AssertProblemAsync(unknownEmail, HttpStatusCode.Unauthorized, "User.InvalidCredentials");
    }

    [Fact]
    public async Task CreatingAUserWithoutATokenIsUnauthorized()
    {
        using HttpResponseMessage response = await _client.PostAsJsonAsync(UsersRoute, NewUser());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ViewerCannotCreateUsersAndAdminCan()
    {
        TokenResponse viewer = await SignupAsync();
        using HttpRequestMessage viewerRequest = Authorized(HttpMethod.Post, UsersRoute, viewer.AccessToken, NewUser());
        using HttpResponseMessage forbidden = await _client.SendAsync(viewerRequest);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        TokenResponse admin = await LoginAsync(ApiFixture.AdminEmail, ApiFixture.AdminPassword);
        using HttpRequestMessage adminRequest = Authorized(HttpMethod.Post, UsersRoute, admin.AccessToken, NewUser());
        using HttpResponseMessage created = await _client.SendAsync(adminRequest);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
    }

    [Fact]
    public async Task RefreshRotatesAndRejectsThePreviousToken()
    {
        TokenResponse first = await SignupAsync();
        using HttpResponseMessage rotated = await _client.PostAsJsonAsync(
            RefreshRoute,
            new { refreshToken = first.RefreshToken });
        TokenResponse second = await ReadTokensAsync(rotated);

        Assert.NotEqual(first.AccessToken, second.AccessToken);
        Assert.NotEqual(first.RefreshToken, second.RefreshToken);

        using HttpResponseMessage reused = await _client.PostAsJsonAsync(
            RefreshRoute,
            new { refreshToken = first.RefreshToken });
        await AssertProblemAsync(reused, HttpStatusCode.Unauthorized, "Auth.InvalidRefreshToken");
    }

    [Fact]
    public async Task ReusedRefreshTokenRevokesTheReplacement()
    {
        TokenResponse first = await SignupAsync();
        using HttpResponseMessage rotated = await _client.PostAsJsonAsync(
            RefreshRoute,
            new { refreshToken = first.RefreshToken });
        TokenResponse second = await ReadTokensAsync(rotated);

        using HttpResponseMessage reused = await _client.PostAsJsonAsync(
            RefreshRoute,
            new { refreshToken = first.RefreshToken });
        await AssertProblemAsync(reused, HttpStatusCode.Unauthorized, "Auth.InvalidRefreshToken");

        using HttpResponseMessage replacement = await _client.PostAsJsonAsync(
            RefreshRoute,
            new { refreshToken = second.RefreshToken });
        await AssertProblemAsync(replacement, HttpStatusCode.Unauthorized, "Auth.InvalidRefreshToken");
    }

    [Fact]
    public async Task LogoutRejectsTheAccessToken()
    {
        TokenResponse tokens = await SignupAsync();
        using HttpRequestMessage logout = Authorized(
            HttpMethod.Post,
            LogoutRoute,
            tokens.AccessToken,
            new { refreshToken = tokens.RefreshToken });
        using HttpResponseMessage loggedOut = await _client.SendAsync(logout);
        Assert.Equal(HttpStatusCode.NoContent, loggedOut.StatusCode);

        using HttpRequestMessage create = Authorized(HttpMethod.Post, UsersRoute, tokens.AccessToken, NewUser());
        using HttpResponseMessage denied = await _client.SendAsync(create);
        Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
    }

    [Fact]
    public async Task DeletedUserCannotLogIn()
    {
        TokenResponse admin = await LoginAsync(ApiFixture.AdminEmail, ApiFixture.AdminPassword);
        string email = UniqueEmail();
        using HttpRequestMessage create = Authorized(
            HttpMethod.Post,
            UsersRoute,
            admin.AccessToken,
            new { firstName = "Del", lastName = "User", email, password = UserPassword });
        using HttpResponseMessage created = await _client.SendAsync(create);
        CreatedUser user = await ReadCreatedAsync(created);

        using HttpRequestMessage delete = Authorized(HttpMethod.Delete, $"{UsersRoute}/{user.Id}", admin.AccessToken);
        using HttpResponseMessage deleted = await _client.SendAsync(delete);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        using HttpResponseMessage login = await _client.PostAsJsonAsync(
            LoginRoute,
            new { email, password = UserPassword });
        await AssertProblemAsync(login, HttpStatusCode.Unauthorized, "User.InvalidCredentials");
    }

    private async Task<TokenResponse> LoginAsync(string email, string password)
    {
        using HttpResponseMessage response = await _client.PostAsJsonAsync(LoginRoute, new { email, password });
        return await ReadTokensAsync(response);
    }

    private async Task<TokenResponse> SignupAsync()
    {
        using HttpResponseMessage response = await _client.PostAsJsonAsync(
            SignupRoute,
            new
            {
                firstName = "View",
                lastName = "User",
                email = UniqueEmail(),
                password = UserPassword
            });
        string body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.Created, body);
        return ReadTokens(body);
    }

    private static async Task<TokenResponse> ReadTokensAsync(HttpResponseMessage response)
    {
        string body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, body);
        return ReadTokens(body);
    }

    private static TokenResponse ReadTokens(string body)
    {
        TokenResponse? tokens = JsonSerializer.Deserialize<TokenResponse>(body, JsonOptions);
        Assert.NotNull(tokens);
        return tokens;
    }

    private static async Task<CreatedUser> ReadCreatedAsync(HttpResponseMessage response)
    {
        string body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.Created, body);
        CreatedUser? created = JsonSerializer.Deserialize<CreatedUser>(body, JsonOptions);
        Assert.NotNull(created);
        return created;
    }

    private static async Task AssertProblemAsync(
        HttpResponseMessage response,
        HttpStatusCode status,
        string code)
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

    private sealed record TokenResponse(string AccessToken, string RefreshToken, DateTime AccessExpiresAt);

    private sealed record CreatedUser(Guid Id);
}
