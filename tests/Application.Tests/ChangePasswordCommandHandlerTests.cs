using Application.Features.Auth.Commands.ChangePassword;
using Application.Tests.Fakes;
using Domain.Features.Users;
using ErrorOr;
using Xunit;

namespace Application.Tests;

public sealed class ChangePasswordCommandHandlerTests
{
    [Fact]
    public async Task ChangePasswordRejectsABadCurrentPassword()
    {
        (User user, FakeUserRepository users, FakeUserContext actor) setup = Setup();
        var sessions = new FakeUserSessionService();

        ErrorOr<Success> result = await ChangePasswordCommandHandler.HandleAsync(
            new ChangePasswordCommand("wrong", "long-password"),
            setup.actor,
            setup.users,
            new FakePasswordHasher(),
            sessions,
            CancellationToken.None);

        ErrorAssert.Code(result, "User.InvalidCredentials");
        Assert.Equal("hashed:current", setup.user.PasswordHash);
        Assert.Equal(0, sessions.RevokeAllCalls);
    }

    [Fact]
    public async Task ChangePasswordRejectsAShortNewPassword()
    {
        (User user, FakeUserRepository users, FakeUserContext actor) setup = Setup();
        var sessions = new FakeUserSessionService();

        ErrorOr<Success> result = await ChangePasswordCommandHandler.HandleAsync(
            new ChangePasswordCommand("current", "short"),
            setup.actor,
            setup.users,
            new FakePasswordHasher(),
            sessions,
            CancellationToken.None);

        ErrorAssert.Code(result, "User.InvalidPassword");
        Assert.Equal("hashed:current", setup.user.PasswordHash);
        Assert.Equal(0, sessions.RevokeAllCalls);
    }

    [Fact]
    public async Task ChangePasswordStoresTheHashAndRevokesSessions()
    {
        (User user, FakeUserRepository users, FakeUserContext actor) setup = Setup();
        var sessions = new FakeUserSessionService();

        ErrorOr<Success> result = await ChangePasswordCommandHandler.HandleAsync(
            new ChangePasswordCommand("current", "long-password"),
            setup.actor,
            setup.users,
            new FakePasswordHasher(),
            sessions,
            CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal("hashed:long-password", setup.user.PasswordHash);
        Assert.Equal(setup.user.Id, setup.user.UpdatedBy);
        Assert.Same(setup.user, setup.users.PasswordUpdated);
        Assert.Equal(1, sessions.RevokeAllCalls);
        Assert.Equal(setup.user.Id, sessions.RevokedAllUserId);
    }

    private static (User User, FakeUserRepository Users, FakeUserContext Actor) Setup()
    {
        User user = TestUsers.Active();
        var users = new FakeUserRepository();
        users.Add(user);
        var actor = new FakeUserContext { UserId = user.Id };
        return (user, users, actor);
    }
}
