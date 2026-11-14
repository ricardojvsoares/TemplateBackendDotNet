using Domain.Authorization;
using Domain.Features.Users;

namespace Application.Tests.Fakes;

internal static class TestUsers
{
    public static User Active(
        string email = "ada@example.com",
        string passwordHash = "hashed:current") =>
        User.Create(
            "Ada",
            "Lovelace",
            email,
            passwordHash,
            GlobalRoles.Viewer,
            [],
            Guid.Empty);
}
