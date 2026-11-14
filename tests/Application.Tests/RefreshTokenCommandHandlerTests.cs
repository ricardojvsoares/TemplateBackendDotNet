using Application.Features.Auth;
using Application.Features.Auth.Commands.Refresh;
using Application.Tests.Fakes;
using ErrorOr;
using Xunit;

namespace Application.Tests;

public sealed class RefreshTokenCommandHandlerTests
{
    [Fact]
    public async Task RefreshForwardsTheRefreshToken()
    {
        var sessions = new FakeUserSessionService();

        ErrorOr<AuthTokensResponse> result = await RefreshTokenCommandHandler.HandleAsync(
            new RefreshTokenCommand("refresh-token"),
            sessions,
            CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal("refresh-token", sessions.RotatedToken);
        Assert.Equal(sessions.Tokens, result.Value);
    }
}
