using ErrorOr;

namespace Domain.Features.Users;

public interface IUserRepository
{
    Task<ErrorOr<Success>> ExistsByEmailAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task<ErrorOr<Guid>> CreateAsync(
        User user,
        CancellationToken cancellationToken = default);
}