using Domain.Features.Users;
using ErrorOr;

namespace Application.Tests.Fakes;

internal sealed class FakeUserRepository : IUserRepository
{
    private readonly List<User> _users = [];

    public Error? GetByEmailError { get; set; }

    public Error? GetByIdError { get; set; }

    public Error? CreateError { get; set; }

    public Error? UpdateError { get; set; }

    public Error? UpdatePasswordError { get; set; }

    public Error? UpdateAccessError { get; set; }

    public Error? DeleteError { get; set; }

    public User? Created { get; private set; }

    public User? Updated { get; private set; }

    public User? PasswordUpdated { get; private set; }

    public User? AccessUpdated { get; private set; }

    public int DeleteCalls { get; private set; }

    public void Add(User user) => _users.Add(user);

    public Task<ErrorOr<User>> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        if (GetByEmailError is Error error)
        {
            return Task.FromResult<ErrorOr<User>>(error);
        }

        User? user = _users.Find(candidate => candidate.Email == email);
        if (user is null)
        {
            return Task.FromResult<ErrorOr<User>>(Error.NotFound());
        }

        return Task.FromResult<ErrorOr<User>>(user);
    }

    public Task<ErrorOr<User>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (GetByIdError is Error error)
        {
            return Task.FromResult<ErrorOr<User>>(error);
        }

        User? user = _users.Find(candidate => candidate.Id == id);
        if (user is null)
        {
            return Task.FromResult<ErrorOr<User>>(Error.NotFound());
        }

        return Task.FromResult<ErrorOr<User>>(user);
    }

    public Task<ErrorOr<Guid>> CreateAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        if (CreateError is Error error)
        {
            return Task.FromResult<ErrorOr<Guid>>(error);
        }

        Created = user;
        _users.Add(user);
        return Task.FromResult<ErrorOr<Guid>>(user.Id);
    }

    public Task<ErrorOr<Success>> UpdateAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        if (UpdateError is Error error)
        {
            return Task.FromResult<ErrorOr<Success>>(error);
        }

        Updated = user;
        return Task.FromResult<ErrorOr<Success>>(Result.Success);
    }

    public Task<ErrorOr<Success>> UpdatePasswordAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        if (UpdatePasswordError is Error error)
        {
            return Task.FromResult<ErrorOr<Success>>(error);
        }

        PasswordUpdated = user;
        return Task.FromResult<ErrorOr<Success>>(Result.Success);
    }

    public Task<ErrorOr<Success>> UpdateAccessAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        if (UpdateAccessError is Error error)
        {
            return Task.FromResult<ErrorOr<Success>>(error);
        }

        AccessUpdated = user;
        return Task.FromResult<ErrorOr<Success>>(Result.Success);
    }

    public Task<ErrorOr<Success>> DeleteAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        DeleteCalls++;
        if (DeleteError is Error error)
        {
            return Task.FromResult<ErrorOr<Success>>(error);
        }

        _users.Remove(user);
        return Task.FromResult<ErrorOr<Success>>(Result.Success);
    }
}
