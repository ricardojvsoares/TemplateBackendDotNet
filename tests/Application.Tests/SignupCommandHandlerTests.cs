using Application.Features.Auth;
using Application.Features.Auth.Commands.Signup;
using Application.Tests.Fakes;
using Domain.Authorization;
using Domain.Features.Users;
using ErrorOr;
using Xunit;

namespace Application.Tests;

public sealed class SignupCommandHandlerTests
{
    [Fact]
    public async Task SignupRejectsAnInvalidProfile()
    {
        var users = new FakeUserRepository();
        var passwords = new FakePasswordHasher();
        var sessions = new FakeUserSessionService();

        ErrorOr<AuthTokensResponse> result = await SignupCommandHandler.HandleAsync(
            new SignupCommand(" ", "Lovelace", "ada@example.com", "long-password"),
            users,
            passwords,
            sessions,
            CancellationToken.None);

        ErrorAssert.Code(result, "User.InvalidData");
        Assert.Empty(passwords.Hashed);
        Assert.Equal(0, sessions.OpenCalls);
    }

    [Fact]
    public async Task SignupRejectsAShortPassword()
    {
        var passwords = new FakePasswordHasher();
        var sessions = new FakeUserSessionService();

        ErrorOr<AuthTokensResponse> result = await SignupCommandHandler.HandleAsync(
            new SignupCommand("Ada", "Lovelace", "ada@example.com", "short"),
            new FakeUserRepository(),
            passwords,
            sessions,
            CancellationToken.None);

        ErrorAssert.Code(result, "User.InvalidPassword");
        Assert.Empty(passwords.Hashed);
        Assert.Equal(0, sessions.OpenCalls);
    }

    [Fact]
    public async Task SignupRejectsADuplicateEmail()
    {
        var users = new FakeUserRepository();
        users.Add(TestUsers.Active());
        var passwords = new FakePasswordHasher();
        var sessions = new FakeUserSessionService();

        ErrorOr<AuthTokensResponse> result = await SignupCommandHandler.HandleAsync(
            new SignupCommand("Ada", "Lovelace", " Ada@Example.com ", "long-password"),
            users,
            passwords,
            sessions,
            CancellationToken.None);

        ErrorAssert.Code(result, "User.AlreadyExists");
        Assert.Contains("ada@example.com", result.FirstError.Description, StringComparison.Ordinal);
        Assert.Empty(passwords.Hashed);
        Assert.Equal(0, sessions.OpenCalls);
    }

    [Fact]
    public async Task SignupReturnsRepositoryErrors()
    {
        var users = new FakeUserRepository { GetByEmailError = Error.Failure("User.Store", "down") };
        var sessions = new FakeUserSessionService();

        ErrorOr<AuthTokensResponse> result = await SignupCommandHandler.HandleAsync(
            new SignupCommand("Ada", "Lovelace", "ada@example.com", "long-password"),
            users,
            new FakePasswordHasher(),
            sessions,
            CancellationToken.None);

        ErrorAssert.Code(result, "User.Store");
        Assert.Equal(0, sessions.OpenCalls);
    }

    [Fact]
    public async Task SignupOpensAViewerSession()
    {
        var users = new FakeUserRepository();
        var passwords = new FakePasswordHasher();
        var sessions = new FakeUserSessionService();

        ErrorOr<AuthTokensResponse> result = await SignupCommandHandler.HandleAsync(
            new SignupCommand("  Ada  ", "  Lovelace  ", "  Ada@Example.com  ", "long-password"),
            users,
            passwords,
            sessions,
            CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal(sessions.Tokens, result.Value);
        User created = users.Created ?? throw new InvalidOperationException("User was not created.");
        Assert.Equal("Ada", created.FirstName);
        Assert.Equal("Lovelace", created.LastName);
        Assert.Equal("ada@example.com", created.Email);
        Assert.Equal(GlobalRoles.Viewer, created.GlobalRole);
        Assert.Empty(created.Permissions);
        Assert.Equal("hashed:long-password", created.PasswordHash);
        Assert.Equal(created.Id, created.CreatedBy);
        Assert.Same(created, sessions.OpenedFor);
    }
}
