using Application.Features.Auth.Commands.Logout;
using Application.Tests.Fakes;
using ErrorOr;
using Xunit;

namespace Application.Tests;

public sealed class LogoutCommandHandlerTests
{
    [Fact]
    public async Task LogoutForwardsTheCurrentToken()
    {
        var context = new FakeUserContext
        {
            TokenId = "access-jti",
            TokenExpiresAt = new DateTime(2026, 1, 2, 4, 0, 0, DateTimeKind.Utc)
        };
        var sessions = new FakeUserSessionService();

        ErrorOr<Success> result = await LogoutCommandHandler.HandleAsync(
            new LogoutCommand("refresh-token"),
            context,
            sessions,
            CancellationToken.None);

        Assert.False(result.IsError);
        Assert.NotNull(sessions.RevokedCurrent);
        Assert.Equal(context.UserId, sessions.RevokedCurrent.UserId);
        Assert.Equal("refresh-token", sessions.RevokedCurrent.RefreshToken);
        Assert.Equal("access-jti", sessions.RevokedCurrent.AccessJti);
        Assert.Equal(context.TokenExpiresAt, sessions.RevokedCurrent.AccessExpiresAt);
    }
}
