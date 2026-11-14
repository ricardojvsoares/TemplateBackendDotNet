using Domain.Authorization;
using Domain.Features.Users;
using ErrorOr;
using Xunit;

namespace Domain.Tests;

public sealed class UserRulesTests
{
    [Fact]
    public void NormalizeEmailTrimsAndLowercases()
    {
        Assert.Equal("ada@example.com", UserRules.NormalizeEmail("  Ada@Example.COM "));
    }

    [Fact]
    public void ValidateProfileRejectsBlankFields()
    {
        ErrorAssert.Code(UserRules.ValidateProfile(null, "Lovelace", "ada@example.com"), "User.InvalidData");
        ErrorAssert.Code(UserRules.ValidateProfile("Ada", " ", "ada@example.com"), "User.InvalidData");
        ErrorAssert.Code(UserRules.ValidateProfile("Ada", "Lovelace", null), "User.InvalidData");
    }

    [Fact]
    public void ValidateProfileAcceptsRequiredFields()
    {
        Assert.False(UserRules.ValidateProfile("Ada", "Lovelace", "ada@example.com").IsError);
    }

    [Fact]
    public void ValidatePasswordRejectsOutOfRangeValues()
    {
        ErrorAssert.Code(UserRules.ValidatePassword(null), "User.InvalidPassword");
        ErrorAssert.Code(UserRules.ValidatePassword(string.Empty), "User.InvalidPassword");
        ErrorAssert.Code(
            UserRules.ValidatePassword(new string('a', UserRules.MinimumPasswordLength - 1)),
            "User.InvalidPassword");
        ErrorAssert.Code(
            UserRules.ValidatePassword(new string('a', UserRules.MaximumPasswordLength + 1)),
            "User.InvalidPassword");
    }

    [Fact]
    public void ValidatePasswordAcceptsBoundaryLengths()
    {
        Assert.False(UserRules.ValidatePassword(new string('a', UserRules.MinimumPasswordLength)).IsError);
        Assert.False(UserRules.ValidatePassword(new string('a', UserRules.MaximumPasswordLength)).IsError);
    }

    [Fact]
    public void ValidateGlobalRoleTreatsNullAsNoRole()
    {
        ErrorOr<GlobalRoleSelection> result = UserRules.ValidateGlobalRole(null);

        Assert.False(result.IsError);
        Assert.Null(result.Value.Role);
    }

    [Fact]
    public void ValidateGlobalRoleRejectsBlankAndUnknownRoles()
    {
        ErrorAssert.Code(UserRules.ValidateGlobalRole("   "), "User.InvalidRole");
        ErrorAssert.Code(UserRules.ValidateGlobalRole("owner"), "User.InvalidRole");
    }

    [Fact]
    public void ValidateGlobalRoleNormalizesAKnownRole()
    {
        ErrorOr<GlobalRoleSelection> result = UserRules.ValidateGlobalRole(" Admin ");

        Assert.False(result.IsError);
        Assert.Equal(GlobalRoles.Admin, result.Value.Role);
    }

    [Fact]
    public void ValidatePermissionsAcceptsEmptyInput()
    {
        ErrorOr<string[]> none = UserRules.ValidatePermissions(null);
        ErrorOr<string[]> empty = UserRules.ValidatePermissions([]);

        Assert.False(none.IsError);
        Assert.Empty(none.Value);
        Assert.False(empty.IsError);
        Assert.Empty(empty.Value);
    }

    [Fact]
    public void ValidatePermissionsRejectsUnknownAndBlankValues()
    {
        ErrorAssert.Code(UserRules.ValidatePermissions(["nope"]), "User.InvalidPermission");
        ErrorAssert.Code(UserRules.ValidatePermissions(["  "]), "User.InvalidPermission");
    }

    [Fact]
    public void ValidatePermissionsTrimsAndDropsDuplicates()
    {
        ErrorOr<string[]> result = UserRules.ValidatePermissions(
            ["  read:users  ", UserPolicies.Read, UserPolicies.Create]);

        Assert.False(result.IsError);
        Assert.Equal([UserPolicies.Read, UserPolicies.Create], result.Value);
    }
}
