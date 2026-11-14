namespace Domain.Features.Users;

public static class UserPolicies
{
    public const string Resource = "users";

    public const string ReadAction = "read";

    public const string ReadDetailsAction = "read-details";

    public const string ReadSessionsAction = "read-sessions";

    public const string CreateAction = "create";

    public const string UpdateAction = "update";

    public const string DeleteAction = "delete";

    public const string Read = $"{ReadAction}:{Resource}";

    public const string ReadDetails = $"{ReadDetailsAction}:{Resource}";

    public const string ReadSessions = $"{ReadSessionsAction}:{Resource}";

    public const string Create = $"{CreateAction}:{Resource}";

    public const string Update = $"{UpdateAction}:{Resource}";

    public const string Delete = $"{DeleteAction}:{Resource}";
}
