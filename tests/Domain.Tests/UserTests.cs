using Domain.Authorization;
using Domain.Features.Users;
using Xunit;

namespace Domain.Tests;

public sealed class UserTests
{
    [Fact]
    public void CanSignInWhenTheUserIsActive()
    {
        User user = ActiveUser();

        Assert.True(user.CanSignIn);
    }

    [Fact]
    public void CanSignInIsFalseWhenInactive()
    {
        User user = ActiveUser();
        user.IsActive = false;

        Assert.False(user.CanSignIn);
    }

    [Fact]
    public void CanSignInIsFalseWhenDeleted()
    {
        User user = ActiveUser();
        user.DeletedAt = DateTime.UtcNow;

        Assert.False(user.CanSignIn);
    }

    [Fact]
    public void CanSignInIsFalseWhenTheHashIsEmpty()
    {
        User user = User.Create(
            "Ada",
            "Lovelace",
            "ada@example.com",
            string.Empty,
            GlobalRoles.Viewer,
            [],
            Guid.Empty);

        Assert.False(user.CanSignIn);
    }

    [Fact]
    public void DeleteMarksTheUserInactive()
    {
        User user = ActiveUser();
        Guid actor = Guid.CreateVersion7();

        user.Delete(actor);

        Assert.False(user.IsActive);
        Assert.NotNull(user.DeletedAt);
        Assert.Equal(actor, user.DeletedBy);
        Assert.Equal(user.DeletedAt, user.UpdatedAt);
        Assert.Equal(actor, user.UpdatedBy);
    }

    private static User ActiveUser() =>
        User.Create(
            "Ada",
            "Lovelace",
            "ada@example.com",
            "hash",
            GlobalRoles.Viewer,
            [],
            Guid.Empty);
}
