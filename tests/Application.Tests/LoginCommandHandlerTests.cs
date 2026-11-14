using Application.Features.Auth;
using Application.Features.Auth.Commands.Login;
using Application.Tests.Fakes;
using Domain.Features.Users;
using ErrorOr;
using Xunit;

namespace Application.Tests;

public sealed class LoginCommandHandlerTests
{
    [Fact]
    public async Task LoginRejectsBlankEmail()
    {
        var users = new FakeUserRepository { GetByEmailError = Error.Failure("User.Store", "down") };
        var sessions = new FakeUserSessionService();

        ErrorOr<AuthTokensResponse> result = await LoginCommandHandler.HandleAsync(
            new LoginCommand("  ", "secret"),
            users,
            new FakePasswordHasher(),
            sessions,
            CancellationToken.None);

        ErrorAssert.Code(result, "User.InvalidCredentials");
        Assert.Equal(0, sessions.OpenCalls);
    }

    [Fact]
    public async Task LoginRejectsEmptyPassword()
    {
        var users = new FakeUserRepository { GetByEmailError = Error.Failure("User.Store", "down") };
        var sessions = new FakeUserSessionService();

        ErrorOr<AuthTokensResponse> result = await LoginCommandHandler.HandleAsync(
            new LoginCommand("ada@example.com", string.Empty),
            users,
            new FakePasswordHasher(),
            sessions,
            CancellationToken.None);

        ErrorAssert.Code(result, "User.InvalidCredentials");
        Assert.Equal(0, sessions.OpenCalls);
    }

    [Fact]
    public async Task LoginRejectsAMissingUser()
    {
        var sessions = new FakeUserSessionService();

        ErrorOr<AuthTokensResponse> result = await LoginCommandHandler.HandleAsync(
            new LoginCommand("ada@example.com", "secret"),
            new FakeUserRepository(),
            new FakePasswordHasher(),
            sessions,
            CancellationToken.None);

        ErrorAssert.Code(result, "User.InvalidCredentials");
        Assert.Equal(0, sessions.OpenCalls);
    }

    [Fact]
    public async Task LoginRejectsABadPassword()
    {
        var users = new FakeUserRepository();
        users.Add(TestUsers.Active());
        var sessions = new FakeUserSessionService();

        ErrorOr<AuthTokensResponse> result = await LoginCommandHandler.HandleAsync(
            new LoginCommand("ada@example.com", "secret"),
            users,
            new FakePasswordHasher(),
            sessions,
            CancellationToken.None);

        ErrorAssert.Code(result, "User.InvalidCredentials");
        Assert.Equal(0, sessions.OpenCalls);
    }

    [Fact]
    public async Task LoginRejectsAUserWhoCannotSignIn()
    {
        User user = TestUsers.Active(passwordHash: "hashed:secret");
        user.IsActive = false;
        var users = new FakeUserRepository();
        users.Add(user);
        var sessions = new FakeUserSessionService();

        ErrorOr<AuthTokensResponse> result = await LoginCommandHandler.HandleAsync(
            new LoginCommand("ada@example.com", "secret"),
            users,
            new FakePasswordHasher(),
            sessions,
            CancellationToken.None);

        ErrorAssert.Code(result, "User.InvalidCredentials");
        Assert.Equal(0, sessions.OpenCalls);
    }

    [Fact]
    public async Task LoginReturnsRepositoryErrors()
    {
        var users = new FakeUserRepository { GetByEmailError = Error.Failure("User.Store", "down") };
        var sessions = new FakeUserSessionService();

        ErrorOr<AuthTokensResponse> result = await LoginCommandHandler.HandleAsync(
            new LoginCommand("ada@example.com", "secret"),
            users,
            new FakePasswordHasher(),
            sessions,
            CancellationToken.None);

        ErrorAssert.Code(result, "User.Store");
        Assert.Equal(0, sessions.OpenCalls);
    }

    [Fact]
    public async Task LoginOpensASessionWhenThePasswordMatches()
    {
        User user = TestUsers.Active(passwordHash: "hashed:secret");
        var users = new FakeUserRepository();
        users.Add(user);
        var sessions = new FakeUserSessionService();

        ErrorOr<AuthTokensResponse> result = await LoginCommandHandler.HandleAsync(
            new LoginCommand("  Ada@Example.com  ", "secret"),
            users,
            new FakePasswordHasher(),
            sessions,
            CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal(sessions.Tokens, result.Value);
        Assert.Equal(1, sessions.OpenCalls);
        Assert.Same(user, sessions.OpenedFor);
    }
}
