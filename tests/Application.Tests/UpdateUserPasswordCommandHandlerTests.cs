using Application.Features.Users.Commands.UpdateUserPassword;
using Application.Tests.Fakes;
using Domain.Features.Users;
using ErrorOr;
using Xunit;

namespace Application.Tests;

public sealed class UpdateUserPasswordCommandHandlerTests
{
    [Fact]
    public async Task UpdateUserPasswordRejectsAShortPassword()
    {
        var users = new FakeUserRepository { GetByIdError = Error.Failure("User.Store", "down") };
        var sessions = new FakeUserSessionService();

        ErrorOr<Success> result = await UpdateUserPasswordCommandHandler.HandleAsync(
            new UpdateUserPasswordCommand(Guid.CreateVersion7(), "short"),
            new FakeUserContext(),
            users,
            new FakePasswordHasher(),
            sessions,
            CancellationToken.None);

        ErrorAssert.Code(result, "User.InvalidPassword");
        Assert.Equal(0, sessions.RevokeAllCalls);
    }

    [Fact]
    public async Task UpdateUserPasswordStoresTheHashAndRevokesSessions()
    {
        User user = TestUsers.Active();
        var users = new FakeUserRepository();
        users.Add(user);
        var actor = new FakeUserContext();
        var sessions = new FakeUserSessionService();

        ErrorOr<Success> result = await UpdateUserPasswordCommandHandler.HandleAsync(
            new UpdateUserPasswordCommand(user.Id, "long-password"),
            actor,
            users,
            new FakePasswordHasher(),
            sessions,
            CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal("hashed:long-password", user.PasswordHash);
        Assert.Equal(actor.UserId, user.UpdatedBy);
        Assert.Same(user, users.PasswordUpdated);
        Assert.Equal(1, sessions.RevokeAllCalls);
        Assert.Equal(user.Id, sessions.RevokedAllUserId);
    }
}
