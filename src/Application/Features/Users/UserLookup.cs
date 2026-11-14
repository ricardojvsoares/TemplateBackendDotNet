using Domain.Features.Users;
using ErrorOr;

namespace Application.Features.Users;

internal static class UserLookup
{
    public static ErrorOr<User> RequireManageable(ErrorOr<User> loaded, Guid id)
    {
        if (loaded.IsError)
        {
            if (loaded.FirstError.Type == ErrorType.NotFound)
            {
                return UserErrors.NotFound(id);
            }

            return loaded.Errors;
        }

        User user = loaded.Value;
        if (!user.IsActive || user.DeletedAt is not null)
        {
            return UserErrors.NotFound(id);
        }

        return user;
    }
}
