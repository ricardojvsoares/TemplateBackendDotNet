using Domain.Authorization;
using ErrorOr;

namespace Domain.Features.Users;

public readonly record struct GlobalRoleSelection(string? Role);

public static class UserRules
{
    public const int MinimumPasswordLength = 8;

    public const int MaximumPasswordLength = 128;

    public static string NormalizeEmail(string email)
    {
#pragma warning disable CA1308
        return email.Trim().ToLowerInvariant();
#pragma warning restore CA1308
    }

    public static ErrorOr<Success> ValidateProfile(string? firstName, string? lastName, string? email)
    {
        if (string.IsNullOrWhiteSpace(firstName)
            || string.IsNullOrWhiteSpace(lastName)
            || string.IsNullOrWhiteSpace(email))
        {
            return UserErrors.InvalidData();
        }

        return Result.Success;
    }

    public static ErrorOr<Success> ValidatePassword(string? password)
    {
        if (string.IsNullOrEmpty(password)
            || password.Length < MinimumPasswordLength
            || password.Length > MaximumPasswordLength)
        {
            return UserErrors.InvalidPassword();
        }

        return Result.Success;
    }

    public static ErrorOr<GlobalRoleSelection> ValidateGlobalRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            if (role is null)
            {
                return new GlobalRoleSelection(null);
            }

            return UserErrors.InvalidRole(role);
        }

#pragma warning disable CA1308
        string normalized = role.Trim().ToLowerInvariant();
#pragma warning restore CA1308
        if (!GlobalRoles.IsDefined(normalized))
        {
            return UserErrors.InvalidRole(role);
        }

        return new GlobalRoleSelection(normalized);
    }

    public static ErrorOr<string[]> ValidatePermissions(IReadOnlyList<string>? permissions)
    {
        if (permissions is null || permissions.Count == 0)
        {
            return Array.Empty<string>();
        }

        var normalized = new List<string>(permissions.Count);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (string permission in permissions)
        {
            if (string.IsNullOrWhiteSpace(permission))
            {
                return UserErrors.InvalidPermission(permission ?? string.Empty);
            }

            string value = permission.Trim();
            if (!PermissionCatalog.Contains(value))
            {
                return UserErrors.InvalidPermission(value);
            }

            if (seen.Add(value))
            {
                normalized.Add(value);
            }
        }

        return normalized.ToArray();
    }
}
