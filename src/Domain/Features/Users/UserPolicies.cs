namespace Domain.Features.Users;

public static class UserPolicies
{
    public const string Read = "read:users";
    public const string ReadDetails = "read-details:users";
    public const string ReadSessions = "read-sessions:users";
    public const string Create = "create:users";
    public const string Update = "update:users";
    public const string Delete = "delete:users";
}
