using Application.Features.Users.Commands.CreateUser;
using Application.Tests.Fakes;
using Domain.Authorization;
using Domain.Features.Users;
using ErrorOr;
using Xunit;

namespace Application.Tests;

public sealed class CreateUserCommandHandlerTests
{
    [Fact]
    public async Task CreateUserStoresAViewerAndReturnsTheId()
    {
        var users = new FakeUserRepository();
        var passwords = new FakePasswordHasher();
        var actor = new FakeUserContext();

        ErrorOr<CreateUserResponse> result = await CreateUserCommandHandler.HandleAsync(
            new CreateUserCommand(
                "  Ada  ",
                "  Lovelace  ",
                "  Ada@Example.com  ",
                "long-password",
                false,
                null,
                null),
            actor,
            users,
            passwords,
            CancellationToken.None);

        Assert.False(result.IsError);
        User created = users.Created ?? throw new InvalidOperationException("User was not created.");
        Assert.Equal(created.Id, result.Value.Id);
        Assert.Equal("Ada", created.FirstName);
        Assert.Equal("Lovelace", created.LastName);
        Assert.Equal("ada@example.com", created.Email);
        Assert.Equal(GlobalRoles.Viewer, created.GlobalRole);
        Assert.Empty(created.Permissions);
        Assert.Equal("hashed:long-password", created.PasswordHash);
        Assert.Equal(["long-password"], passwords.Hashed);
        Assert.Equal(actor.UserId, created.CreatedBy);
    }

    [Fact]
    public async Task CreateUserRejectsAnUnknownRole()
    {
        var users = new FakeUserRepository();

        ErrorOr<CreateUserResponse> result = await CreateUserCommandHandler.HandleAsync(
            new CreateUserCommand("Ada", "Lovelace", "ada@example.com", "long-password", true, "owner", null),
            new FakeUserContext(),
            users,
            new FakePasswordHasher(),
            CancellationToken.None);

        ErrorAssert.Code(result, "User.InvalidRole");
        Assert.Null(users.Created);
    }

    [Fact]
    public async Task CreateUserRejectsAnUnknownPermission()
    {
        var users = new FakeUserRepository();

        ErrorOr<CreateUserResponse> result = await CreateUserCommandHandler.HandleAsync(
            new CreateUserCommand(
                "Ada",
                "Lovelace",
                "ada@example.com",
                "long-password",
                false,
                null,
                ["nope"]),
            new FakeUserContext(),
            users,
            new FakePasswordHasher(),
            CancellationToken.None);

        ErrorAssert.Code(result, "User.InvalidPermission");
        Assert.Null(users.Created);
    }

    [Fact]
    public async Task CreateUserRejectsADuplicateEmail()
    {
        var users = new FakeUserRepository();
        users.Add(TestUsers.Active());

        ErrorOr<CreateUserResponse> result = await CreateUserCommandHandler.HandleAsync(
            new CreateUserCommand("Ada", "Lovelace", "Ada@Example.com", "long-password", false, null, null),
            new FakeUserContext(),
            users,
            new FakePasswordHasher(),
            CancellationToken.None);

        ErrorAssert.Code(result, "User.AlreadyExists");
        Assert.Contains("ada@example.com", result.FirstError.Description, StringComparison.Ordinal);
        Assert.Null(users.Created);
    }
}
