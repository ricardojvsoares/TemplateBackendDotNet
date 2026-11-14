using Application.Abstractions.Identity;
using Application.Features.Auth;
using Application.Tests.Fakes;
using Domain.Features.Users;
using ErrorOr;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Application.Tests;

public sealed class UserSessionServiceTests
{
    private readonly DateTimeOffset _now = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);

    private readonly FixedTimeProvider _time;

    private readonly FakeTokenService _tokens = new();

    private readonly FakeRefreshTokenStore _store = new();

    private readonly FakeUserRepository _users = new();

    private readonly FakeCacheService _cache = new();

    public UserSessionServiceTests() => _time = new FixedTimeProvider(_now);

    [Fact]
    public async Task OpenStoresTheRefreshHash()
    {
        User user = TestUsers.Active();
        UserSessionService service = CreateService();

        ErrorOr<AuthTokensResponse> result = await service.OpenAsync(user, CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal("access-1", result.Value.AccessToken);
        Assert.Equal("refresh-1", result.Value.RefreshToken);
        Assert.Equal(_tokens.AccessExpiresAt, result.Value.AccessExpiresAt);
        RefreshToken stored = Assert.Single(_store.Tokens);
        Assert.Equal(user.Id, stored.UserId);
        Assert.Equal("hash:refresh-1", stored.TokenHash);
        Assert.Equal("jti-1", stored.AccessJti);
        Assert.Equal(_tokens.AccessExpiresAt, stored.AccessExpiresAt);
        Assert.Equal(_tokens.RefreshExpiresAt, stored.ExpiresAt);
        Assert.Equal(_now.UtcDateTime, stored.CreatedAt);
    }

    [Fact]
    public async Task OpenReturnsStoreErrors()
    {
        _store.CreateError = Error.Failure("Auth.Store", "down");

        ErrorOr<AuthTokensResponse> result = await CreateService().OpenAsync(TestUsers.Active(), CancellationToken.None);

        ErrorAssert.Code(result, "Auth.Store");
        Assert.Empty(_store.Tokens);
    }

    [Fact]
    public async Task RotateRejectsABlankToken()
    {
        ErrorOr<AuthTokensResponse> result = await CreateService().RotateAsync("   ", CancellationToken.None);

        ErrorAssert.Code(result, "Auth.InvalidRefreshToken");
        Assert.Equal(0, _store.RevokeAllCalls);
    }

    [Fact]
    public async Task RotateRejectsAMissingToken()
    {
        ErrorOr<AuthTokensResponse> result = await CreateService().RotateAsync("missing", CancellationToken.None);

        ErrorAssert.Code(result, "Auth.InvalidRefreshToken");
        Assert.Equal(0, _store.RevokeAllCalls);
    }

    [Fact]
    public async Task RotateReturnsStoreErrors()
    {
        _store.GetError = Error.Failure("Auth.Store", "down");

        ErrorOr<AuthTokensResponse> result = await CreateService().RotateAsync("old", CancellationToken.None);

        ErrorAssert.Code(result, "Auth.Store");
        Assert.Equal(0, _store.RevokeAllCalls);
    }

    [Fact]
    public async Task RotateRejectsAnExpiredToken()
    {
        User user = TestUsers.Active();
        RefreshToken token = SeedToken(user, "old", expiresAt: _now.UtcDateTime);

        ErrorOr<AuthTokensResponse> result = await CreateService().RotateAsync("old", CancellationToken.None);

        ErrorAssert.Code(result, "Auth.InvalidRefreshToken");
        Assert.Null(token.RevokedAt);
        Assert.Equal(0, _store.RevokeAllCalls);
    }

    [Fact]
    public async Task RevokedRefreshTokenRevokesEverySession()
    {
        User user = TestUsers.Active();
        SeedToken(user, "old", revokedAt: _now.UtcDateTime.AddMinutes(-1));
        RefreshToken other = SeedToken(user, "other", accessJti: "other-jti");

        ErrorOr<AuthTokensResponse> result = await CreateService().RotateAsync("old", CancellationToken.None);

        ErrorAssert.Code(result, "Auth.InvalidRefreshToken");
        Assert.Equal(1, _store.RevokeAllCalls);
        Assert.NotNull(other.RevokedAt);
        Assert.Equal("1", _cache.Items[AuthCacheKeys.Deny("other-jti")].Value);
    }

    [Fact]
    public async Task LostRotateRaceRevokesEverySession()
    {
        User user = TestUsers.Active();
        _users.Add(user);
        RefreshToken token = SeedToken(user, "old", accessJti: "old-jti");
        _store.TryRevokeSucceeds = false;

        ErrorOr<AuthTokensResponse> result = await CreateService().RotateAsync("old", CancellationToken.None);

        ErrorAssert.Code(result, "Auth.InvalidRefreshToken");
        Assert.Equal(1, _store.RevokeAllCalls);
        Assert.NotNull(token.RevokedAt);
        Assert.Equal("1", _cache.Items[AuthCacheKeys.Deny("old-jti")].Value);
    }

    [Fact]
    public async Task RotateRevokesAUserWhoCannotSignIn()
    {
        User user = TestUsers.Active();
        user.IsActive = false;
        _users.Add(user);
        RefreshToken token = SeedToken(user, "old");

        ErrorOr<AuthTokensResponse> result = await CreateService().RotateAsync("old", CancellationToken.None);

        ErrorAssert.Code(result, "Auth.InvalidRefreshToken");
        Assert.Equal(1, _store.RevokeAllCalls);
        Assert.NotNull(token.RevokedAt);
    }

    [Fact]
    public async Task RotateRejectsAMissingUser()
    {
        User user = TestUsers.Active();
        RefreshToken token = SeedToken(user, "old");

        ErrorOr<AuthTokensResponse> result = await CreateService().RotateAsync("old", CancellationToken.None);

        ErrorAssert.Code(result, "Auth.InvalidRefreshToken");
        Assert.Null(token.RevokedAt);
        Assert.Equal(0, _store.RevokeAllCalls);
    }

    [Fact]
    public async Task RotateReturnsUserLookupErrors()
    {
        User user = TestUsers.Active();
        RefreshToken token = SeedToken(user, "old");
        _users.GetByIdError = Error.Failure("User.Store", "down");

        ErrorOr<AuthTokensResponse> result = await CreateService().RotateAsync("old", CancellationToken.None);

        ErrorAssert.Code(result, "User.Store");
        Assert.Null(token.RevokedAt);
        Assert.Equal(0, _store.RevokeAllCalls);
    }

    [Fact]
    public async Task RotateReturnsAStoreErrorWithoutRevokingSessions()
    {
        User user = TestUsers.Active();
        _users.Add(user);
        RefreshToken token = SeedToken(user, "old");
        _store.TryRevokeError = Error.Failure("Auth.Store", "down");

        ErrorOr<AuthTokensResponse> result = await CreateService().RotateAsync("old", CancellationToken.None);

        ErrorAssert.Code(result, "Auth.Store");
        Assert.Null(token.RevokedAt);
        Assert.Equal(0, _store.RevokeAllCalls);
    }

    [Fact]
    public async Task RotateIssuesANewPair()
    {
        User user = TestUsers.Active();
        _users.Add(user);
        RefreshToken current = SeedToken(user, "old");

        ErrorOr<AuthTokensResponse> result = await CreateService().RotateAsync("old", CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal("access-1", result.Value.AccessToken);
        Assert.Equal("refresh-1", result.Value.RefreshToken);
        Assert.Equal(_tokens.AccessExpiresAt, result.Value.AccessExpiresAt);
        Assert.Equal(_now.UtcDateTime, current.RevokedAt);
        RefreshToken created = Assert.Single(_store.Tokens, token => token.TokenHash == "hash:refresh-1");
        Assert.Equal(user.Id, created.UserId);
        Assert.Equal("jti-1", created.AccessJti);
    }

    [Fact]
    public async Task RevokeCurrentIgnoresAnotherUsersToken()
    {
        User owner = TestUsers.Active();
        RefreshToken stored = SeedToken(owner, "other", accessJti: "stored-jti");
        DateTime accessExpiresAt = _now.UtcDateTime.AddMinutes(5);
        Guid caller = Guid.CreateVersion7();

        ErrorOr<Success> result = await CreateService().RevokeCurrentAsync(
            caller,
            "other",
            "caller-jti",
            accessExpiresAt,
            CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Null(stored.RevokedAt);
        Assert.Equal("1", _cache.Items[AuthCacheKeys.Deny("caller-jti")].Value);
        Assert.Equal(TimeSpan.FromMinutes(5), _cache.Items[AuthCacheKeys.Deny("caller-jti")].Ttl);
        Assert.False(_cache.Items.ContainsKey(AuthCacheKeys.Deny("stored-jti")));
    }

    [Fact]
    public async Task RevokeCurrentSkipsAnExpiredAccessToken()
    {
        User user = TestUsers.Active();
        RefreshToken stored = SeedToken(user, "current", accessExpiresAt: _now.UtcDateTime);

        ErrorOr<Success> result = await CreateService().RevokeCurrentAsync(
            user.Id,
            "current",
            "caller-jti",
            _now.UtcDateTime,
            CancellationToken.None);

        Assert.False(result.IsError);
        Assert.NotNull(stored.RevokedAt);
        Assert.Empty(_cache.Items);
    }

    [Fact]
    public async Task RevokeAllDenylistsLiveAccessTokens()
    {
        User user = TestUsers.Active();
        RefreshToken live = SeedToken(user, "live", accessJti: "live-jti");
        RefreshToken blank = SeedToken(user, "blank", accessJti: string.Empty);
        RefreshToken stale = SeedToken(user, "stale", accessJti: "stale-jti", accessExpiresAt: _now.UtcDateTime);

        ErrorOr<Success> result = await CreateService().RevokeAllAsync(user.Id, CancellationToken.None);

        Assert.False(result.IsError);
        Assert.NotNull(live.RevokedAt);
        Assert.NotNull(blank.RevokedAt);
        Assert.NotNull(stale.RevokedAt);
        Assert.Equal("1", _cache.Items[AuthCacheKeys.Deny("live-jti")].Value);
        Assert.False(_cache.Items.ContainsKey(AuthCacheKeys.Deny("stale-jti")));
        Assert.False(_cache.Items.ContainsKey(AuthCacheKeys.Deny(string.Empty)));
    }

    private UserSessionService CreateService() =>
        new(_tokens, _store, _users, _cache, _time, NullLogger<UserSessionService>.Instance);

    private RefreshToken SeedToken(
        User user,
        string rawToken,
        DateTime? expiresAt = null,
        DateTime? revokedAt = null,
        DateTime? accessExpiresAt = null,
        string accessJti = "stored-jti")
    {
        var token = new RefreshToken
        {
            Id = Guid.CreateVersion7(),
            UserId = user.Id,
            TokenHash = _tokens.HashRefreshToken(rawToken),
            AccessJti = accessJti,
            AccessExpiresAt = accessExpiresAt ?? _now.UtcDateTime.AddMinutes(15),
            ExpiresAt = expiresAt ?? _now.UtcDateTime.AddDays(1),
            RevokedAt = revokedAt,
            CreatedAt = _now.UtcDateTime
        };
        _store.Add(token);
        return token;
    }
}
