namespace Domain.Authorization;

public static class GlobalRoles
{
    public const string Viewer = "viewer";

    public const string Maintainer = "maintainer";

    public const string Admin = "admin";

    public static bool IsDefined(string? role) =>
        role is Viewer or Maintainer or Admin;
}
