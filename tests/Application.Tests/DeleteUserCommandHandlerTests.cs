using Application.Features.Users.Commands.DeleteUser;
using Application.Tests.Fakes;
using Domain.Features.Users;
using ErrorOr;
using Xunit;

namespace Application.Tests;

public sealed class DeleteUserCommandHandlerTests
{
    [Fact]
    public async Task DeleteUserReportsAMissingUserAsNotFound()
    {
        var sessions = new FakeUserSessionService();

        ErrorOr<Success> result = await DeleteUserCommandHandler.HandleAsync(
            new DeleteUserCommand(Guid.CreateVersion7()),
            new FakeUserContext(),
            new FakeUserRepository(),
            sessions,
            CancellationToken.None);

        ErrorAssert.Code(result, "User.NotFound");
        Assert.Equal(0, sessions.RevokeAllCalls);
    }

    [Fact]
    public async Task DeleteUserReturnsRepositoryErrors()
    {
        var users = new FakeUserRepository { GetByIdError = Error.Failure("User.Store", "down") };
        var sessions = new FakeUserSessionService();

        ErrorOr<Success> result = await DeleteUserCommandHandler.HandleAsync(
            new DeleteUserCommand(Guid.CreateVersion7()),
            new FakeUserContext(),
            users,
            sessions,
            CancellationToken.None);

        ErrorAssert.Code(result, "User.Store");
        Assert.Equal(0, users.DeleteCalls);
        Assert.Equal(0, sessions.RevokeAllCalls);
    }

    [Fact]
    public async Task DeleteUserDeletesAnActiveUserAndRevokesSessions()
    {
        User user = TestUsers.Active();
        var users = new FakeUserRepository();
        users.Add(user);
        var actor = new FakeUserContext();
        var sessions = new FakeUserSessionService();

        ErrorOr<Success> result = await DeleteUserCommandHandler.HandleAsync(
            new DeleteUserCommand(user.Id),
            actor,
            users,
            sessions,
            CancellationToken.None);

        Assert.False(result.IsError);
        Assert.False(user.IsActive);
        Assert.NotNull(user.DeletedAt);
        Assert.Equal(actor.UserId, user.DeletedBy);
        Assert.Equal(1, users.DeleteCalls);
        Assert.Equal(1, sessions.RevokeAllCalls);
        Assert.Equal(user.Id, sessions.RevokedAllUserId);
    }

    [Fact]
    public async Task DeleteUserSkipsAnInactiveUserAndRevokesSessions()
    {
        User user = TestUsers.Active();
        user.IsActive = false;
        var users = new FakeUserRepository();
        users.Add(user);
        var sessions = new FakeUserSessionService();

        ErrorOr<Success> result = await DeleteUserCommandHandler.HandleAsync(
            new DeleteUserCommand(user.Id),
            new FakeUserContext(),
            users,
            sessions,
            CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal(0, users.DeleteCalls);
        Assert.Equal(1, sessions.RevokeAllCalls);
        Assert.Equal(user.Id, sessions.RevokedAllUserId);
    }

    [Fact]
    public async Task DeleteUserIgnoresAMissingRowAndRevokesSessions()
    {
        User user = TestUsers.Active();
        var users = new FakeUserRepository
        {
            DeleteError = Error.NotFound("User.NotFound", "gone")
        };
        users.Add(user);
        var sessions = new FakeUserSessionService();

        ErrorOr<Success> result = await DeleteUserCommandHandler.HandleAsync(
            new DeleteUserCommand(user.Id),
            new FakeUserContext(),
            users,
            sessions,
            CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal(1, users.DeleteCalls);
        Assert.Equal(1, sessions.RevokeAllCalls);
        Assert.Equal(user.Id, sessions.RevokedAllUserId);
    }
}
