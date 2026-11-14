using Domain.Features.Users;

namespace Domain.Authorization;

public sealed record PermissionResource(
    string Name,
    bool IsSystem,
    IReadOnlyList<string> Actions);

public static class PermissionCatalog
{
    private static readonly object Gate = new();

    private static readonly List<PermissionResource> Resources = [];

    private static readonly HashSet<string> ViewerActions = new(StringComparer.Ordinal)
    {
        UserPolicies.ReadAction,
        UserPolicies.ReadDetailsAction
    };

    private static readonly HashSet<string> MaintainerActions = new(StringComparer.Ordinal)
    {
        UserPolicies.ReadAction,
        UserPolicies.ReadDetailsAction,
        UserPolicies.CreateAction,
        UserPolicies.UpdateAction,
        UserPolicies.DeleteAction
    };

    static PermissionCatalog()
    {
        Register(
            UserPolicies.Resource,
            isSystem: true,
            [
                UserPolicies.ReadAction,
                UserPolicies.ReadDetailsAction,
                UserPolicies.ReadSessionsAction,
                UserPolicies.CreateAction,
                UserPolicies.UpdateAction,
                UserPolicies.DeleteAction
            ]);
    }

    public static void Register(string name, bool isSystem, IReadOnlyList<string> actions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(actions);

        string[] distinct = actions
            .Where(static action => !string.IsNullOrWhiteSpace(action))
            .Select(static action => action.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        lock (Gate)
        {
            if (Resources.Exists(resource => string.Equals(resource.Name, name, StringComparison.Ordinal)))
            {
                return;
            }

            Resources.Add(new PermissionResource(name, isSystem, distinct));
        }
    }

    public static bool Contains(string permission) =>
        Snapshot().Any(resource => resource.Actions.Any(action =>
            string.Equals(Format(action, resource.Name), permission, StringComparison.Ordinal)));

    public static IReadOnlyList<string> Effective(string? globalRole, IReadOnlyList<string> grants)
    {
        var permissions = new HashSet<string>(StringComparer.Ordinal);

        foreach (string permission in BundleFor(globalRole))
        {
            permissions.Add(permission);
        }

        foreach (string grant in grants)
        {
            permissions.Add(grant);
        }

        var ordered = new List<string>(permissions);
        ordered.Sort(StringComparer.Ordinal);
        return ordered;
    }

    public static string Format(string action, string resource) => $"{action}:{resource}";

    private static List<string> BundleFor(string? globalRole)
    {
        if (globalRole == GlobalRoles.Admin)
        {
            return All();
        }

        HashSet<string>? allowed = globalRole switch
        {
            GlobalRoles.Viewer => ViewerActions,
            GlobalRoles.Maintainer => MaintainerActions,
            _ => null
        };

        if (allowed is null)
        {
            return [];
        }

        var permissions = new List<string>();
        foreach (PermissionResource resource in Snapshot().Where(resource => !resource.IsSystem))
        {
            foreach (string action in resource.Actions.Where(allowed.Contains))
            {
                permissions.Add(Format(action, resource.Name));
            }
        }

        return permissions;
    }

    private static List<string> All()
    {
        var permissions = new List<string>();
        foreach (PermissionResource resource in Snapshot())
        {
            foreach (string action in resource.Actions)
            {
                permissions.Add(Format(action, resource.Name));
            }
        }

        return permissions;
    }

    private static PermissionResource[] Snapshot()
    {
        lock (Gate)
        {
            return [.. Resources];
        }
    }
}
