using Domain.Authorization;
using Domain.Features.Users;
using Xunit;

namespace Domain.Tests;

public sealed class PermissionCatalogTests
{
    [Fact]
    public void AdminIncludesTheSystemResource()
    {
        string[] expected =
        [
            UserPolicies.Create,
            UserPolicies.Delete,
            UserPolicies.Read,
            UserPolicies.ReadDetails,
            UserPolicies.ReadSessions,
            UserPolicies.Update
        ];
        Array.Sort(expected, StringComparer.Ordinal);

        Assert.Equal(expected, PermissionCatalog.Effective(GlobalRoles.Admin, [UserPolicies.Read]));
    }

    [Fact]
    public void ViewerAndMaintainerSkipSystemResources()
    {
        Assert.Empty(PermissionCatalog.Effective(GlobalRoles.Viewer, []));
        Assert.Empty(PermissionCatalog.Effective(GlobalRoles.Maintainer, []));
        Assert.Equal(
            [UserPolicies.Update],
            PermissionCatalog.Effective(GlobalRoles.Maintainer, [UserPolicies.Update]));
    }

    [Fact]
    public void EffectiveSortsAndDeduplicatesGrants()
    {
        Assert.Equal(
            [UserPolicies.Create, UserPolicies.Read],
            PermissionCatalog.Effective(GlobalRoles.Viewer, [UserPolicies.Read, UserPolicies.Create, UserPolicies.Read]));
    }
}
