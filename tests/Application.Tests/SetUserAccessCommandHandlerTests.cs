using Application.Features.Users.Commands.SetUserAccess;
using Application.Tests.Fakes;
using Domain.Authorization;
using Domain.Features.Users;
using ErrorOr;
using Xunit;

namespace Application.Tests;

public sealed class SetUserAccessCommandHandlerTests
{
    [Fact]
    public async Task SetUserAccessRejectsAnUnknownRole()
    {
        User user = TestUsers.Active();
        var users = new FakeUserRepository();
        users.Add(user);
        var sessions = new FakeUserSessionService();

        ErrorOr<Success> result = await SetUserAccessCommandHandler.HandleAsync(
            new SetUserAccessCommand(user.Id, "owner", null),
            new FakeUserContext(),
            users,
            sessions,
            CancellationToken.None);

        ErrorAssert.Code(result, "User.InvalidRole");
        Assert.Equal(GlobalRoles.Viewer, user.GlobalRole);
        Assert.Equal(0, sessions.RevokeAllCalls);
    }

    [Fact]
    public async Task SetUserAccessReportsAMissingUserAsNotFound()
    {
        var sessions = new FakeUserSessionService();

        ErrorOr<Success> result = await SetUserAccessCommandHandler.HandleAsync(
            new SetUserAccessCommand(Guid.CreateVersion7(), GlobalRoles.Admin, null),
            new FakeUserContext(),
            new FakeUserRepository(),
            sessions,
            CancellationToken.None);

        ErrorAssert.Code(result, "User.NotFound");
        Assert.Equal(0, sessions.RevokeAllCalls);
    }

    [Fact]
    public async Task SetUserAccessAssignsAccessAndRevokesSessions()
    {
        User user = TestUsers.Active();
        var users = new FakeUserRepository();
        users.Add(user);
        var actor = new FakeUserContext();
        var sessions = new FakeUserSessionService();

        ErrorOr<Success> result = await SetUserAccessCommandHandler.HandleAsync(
            new SetUserAccessCommand(user.Id, " Admin ", [UserPolicies.Read, UserPolicies.Read]),
            actor,
            users,
            sessions,
            CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal(GlobalRoles.Admin, user.GlobalRole);
        Assert.Equal([UserPolicies.Read], user.Permissions);
        Assert.Equal(actor.UserId, user.UpdatedBy);
        Assert.Same(user, users.AccessUpdated);
        Assert.Equal(1, sessions.RevokeAllCalls);
        Assert.Equal(user.Id, sessions.RevokedAllUserId);
    }
}
