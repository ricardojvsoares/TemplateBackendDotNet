namespace Persistence.Features.Users;

internal static class UserQueries
{
    private const string Columns = @"
        id,
        email,
        first_name,
        last_name,
        password_hash,
        global_role,
        permissions,
        is_active,
        created_at,
        created_by,
        updated_at,
        updated_by,
        deleted_at,
        deleted_by";

    public const string GetByEmail = $@"
        SELECT {Columns}
        FROM users
        WHERE LOWER(email) = LOWER(@p_email)
        LIMIT 1";

    public const string GetById = $@"
        SELECT {Columns}
        FROM users
        WHERE id = @p_id
        LIMIT 1";

    public const string Create = @"
        INSERT INTO users
            (id, email, first_name, last_name, password_hash, global_role, permissions, is_active, created_at, created_by)
        VALUES
            (@p_id, @p_email, @p_firstName, @p_lastName, @p_passwordHash, @p_globalRole, @p_permissions, @p_isActive, @p_createdAt, @p_createdBy)
        RETURNING id";

    public const string Update = @"
        UPDATE users
        SET first_name = @p_firstName,
            last_name = @p_lastName,
            updated_at = @p_updatedAt,
            updated_by = @p_updatedBy
        WHERE id = @p_id
          AND deleted_at IS NULL
          AND is_active = true";

    public const string UpdatePassword = @"
        UPDATE users
        SET password_hash = @p_passwordHash,
            updated_at = @p_updatedAt,
            updated_by = @p_updatedBy
        WHERE id = @p_id
          AND deleted_at IS NULL
          AND is_active = true";

    public const string UpdateAccess = @"
        UPDATE users
        SET global_role = @p_globalRole,
            permissions = @p_permissions,
            updated_at = @p_updatedAt,
            updated_by = @p_updatedBy
        WHERE id = @p_id
          AND deleted_at IS NULL
          AND is_active = true";

    public const string Delete = @"
        UPDATE users
        SET is_active = @p_isActive,
            deleted_at = @p_deletedAt,
            deleted_by = @p_deletedBy,
            updated_at = @p_updatedAt,
            updated_by = @p_updatedBy
        WHERE id = @p_id
          AND deleted_at IS NULL";
}
