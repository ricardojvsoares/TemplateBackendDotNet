namespace Persistence.Features.Users;

internal static class UserQueries
{
    public const string ExistsByEmail = @"
        SELECT 1
        FROM users
        WHERE LOWER(email) = LOWER(@p_email)
        LIMIT 1";
    public const string Create = @"
        INSERT INTO users
            (id, email, first_name, last_name, created_at, created_by, is_active)
        VALUES
            (@p_id, @p_email, @p_firstName, @p_lastName, @p_createdAt, @p_createdBy, true)
        RETURNING id";
}
