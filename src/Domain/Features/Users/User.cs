using Domain.Authorization;
using Domain.Common;

namespace Domain.Features.Users;

public class User : Entity
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string? GlobalRole { get; set; }

#pragma warning disable CA1819
    public string[] Permissions { get; set; } = [];
#pragma warning restore CA1819

    public bool CanSignIn => IsActive && DeletedAt is null && PasswordHash.Length > 0;

    public IReadOnlyList<string> EffectivePermissions() =>
        PermissionCatalog.Effective(GlobalRole, Permissions);

    public static User Create(
        string firstName,
        string lastName,
        string email,
        string passwordHash,
        string? globalRole,
        string[] permissions,
        Guid createdBy)
    {
        return new User
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            PasswordHash = passwordHash,
            GlobalRole = globalRole,
            Permissions = permissions,
            CreatedBy = createdBy
        };
    }

    public void Update(
        string firstName,
        string lastName,
        Guid updatedBy)
    {
        FirstName = firstName;
        LastName = lastName;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }

    public void AssignAccess(
        string? globalRole,
        string[] permissions,
        Guid updatedBy)
    {
        GlobalRole = globalRole;
        Permissions = permissions;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }

    public void ChangePassword(string passwordHash, Guid updatedBy)
    {
        PasswordHash = passwordHash;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }

    public void Delete(Guid deletedBy)
    {
        DateTime now = DateTime.UtcNow;
        IsActive = false;
        DeletedAt = now;
        DeletedBy = deletedBy;
        UpdatedAt = now;
        UpdatedBy = deletedBy;
    }
}
