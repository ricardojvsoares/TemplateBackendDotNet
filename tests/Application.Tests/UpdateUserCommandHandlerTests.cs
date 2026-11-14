using Application.Features.Users.Commands.UpdateUser;
using Application.Tests.Fakes;
using Domain.Features.Users;
using ErrorOr;
using Xunit;

namespace Application.Tests;

public sealed class UpdateUserCommandHandlerTests
{
    [Fact]
    public async Task UpdateUserRejectsABlankName()
    {
        User user = TestUsers.Active();
        var users = new FakeUserRepository();
        users.Add(user);

        ErrorOr<Success> result = await UpdateUserCommandHandler.HandleAsync(
            new UpdateUserCommand(user.Id, "  ", "Lovelace"),
            new FakeUserContext(),
            users,
            CancellationToken.None);

        ErrorAssert.Code(result, "User.InvalidData");
        Assert.Null(users.Updated);
        Assert.Equal("Ada", user.FirstName);
    }

    [Fact]
    public async Task UpdateUserReportsAMissingUserAsNotFound()
    {
        var users = new FakeUserRepository();

        ErrorOr<Success> result = await UpdateUserCommandHandler.HandleAsync(
            new UpdateUserCommand(Guid.CreateVersion7(), "Ada", "Lovelace"),
            new FakeUserContext(),
            users,
            CancellationToken.None);

        ErrorAssert.Code(result, "User.NotFound");
        Assert.Null(users.Updated);
    }

    [Fact]
    public async Task UpdateUserReportsAnInactiveUserAsNotFound()
    {
        User user = TestUsers.Active();
        user.IsActive = false;
        var users = new FakeUserRepository();
        users.Add(user);

        ErrorOr<Success> result = await UpdateUserCommandHandler.HandleAsync(
            new UpdateUserCommand(user.Id, "Grace", "Hopper"),
            new FakeUserContext(),
            users,
            CancellationToken.None);

        ErrorAssert.Code(result, "User.NotFound");
        Assert.Null(users.Updated);
        Assert.Equal("Ada", user.FirstName);
    }

    [Fact]
    public async Task UpdateUserTrimsTheName()
    {
        User user = TestUsers.Active();
        var users = new FakeUserRepository();
        users.Add(user);
        var actor = new FakeUserContext();

        ErrorOr<Success> result = await UpdateUserCommandHandler.HandleAsync(
            new UpdateUserCommand(user.Id, "  Grace  ", "  Hopper  "),
            actor,
            users,
            CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal("Grace", user.FirstName);
        Assert.Equal("Hopper", user.LastName);
        Assert.Equal(actor.UserId, user.UpdatedBy);
        Assert.Same(user, users.Updated);
    }
}
