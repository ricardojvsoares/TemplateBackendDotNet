using ErrorOr;

namespace Domain.Features.Users;

public interface IUserRepository
{
    Task<ErrorOr<User>> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task<ErrorOr<User>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ErrorOr<Guid>> CreateAsync(
        User user,
        CancellationToken cancellationToken = default);

    Task<ErrorOr<Success>> UpdateAsync(
        User user,
        CancellationToken cancellationToken = default);

    Task<ErrorOr<Success>> UpdatePasswordAsync(
        User user,
        CancellationToken cancellationToken = default);

    Task<ErrorOr<Success>> UpdateAccessAsync(
        User user,
        CancellationToken cancellationToken = default);

    Task<ErrorOr<Success>> DeleteAsync(
        User user,
        CancellationToken cancellationToken = default);
}
