using Application.Abstractions.Identity;
using ErrorOr;

namespace Application.Tests.Fakes;

internal sealed class FakeRefreshTokenStore : IRefreshTokenStore
{
    private readonly List<RefreshToken> _tokens = [];

    public IReadOnlyList<RefreshToken> Tokens => _tokens;

    public Error? CreateError { get; set; }

    public Error? GetError { get; set; }

    public Error? RevokeAllError { get; set; }

    public Error? TryRevokeError { get; set; }

    public bool TryRevokeSucceeds { get; set; } = true;

    public int RevokeAllCalls { get; private set; }

    public void Add(RefreshToken token) => _tokens.Add(token);

    public Task<ErrorOr<Success>> CreateAsync(
        RefreshToken token,
        CancellationToken cancellationToken = default)
    {
        if (CreateError is Error error)
        {
            return Task.FromResult<ErrorOr<Success>>(error);
        }

        _tokens.Add(token);
        return Task.FromResult<ErrorOr<Success>>(Result.Success);
    }

    public Task<ErrorOr<RefreshToken>> GetByHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        if (GetError is Error error)
        {
            return Task.FromResult<ErrorOr<RefreshToken>>(error);
        }

        RefreshToken? found = _tokens.Find(token => token.TokenHash == tokenHash);
        if (found is null)
        {
            return Task.FromResult<ErrorOr<RefreshToken>>(Error.NotFound());
        }

        return Task.FromResult<ErrorOr<RefreshToken>>(found);
    }

    public Task<ErrorOr<Success>> RevokeAsync(
        Guid id,
        DateTime revokedAt,
        CancellationToken cancellationToken = default)
    {
        RefreshToken? token = _tokens.Find(candidate => candidate.Id == id);
        if (token is null)
        {
            return Task.FromResult<ErrorOr<Success>>(Error.NotFound());
        }

        token.RevokedAt = revokedAt;
        return Task.FromResult<ErrorOr<Success>>(Result.Success);
    }

    public Task<ErrorOr<IReadOnlyList<RefreshToken>>> RevokeAllForUserAsync(
        Guid userId,
        DateTime revokedAt,
        CancellationToken cancellationToken = default)
    {
        RevokeAllCalls++;
        if (RevokeAllError is Error error)
        {
            return Task.FromResult<ErrorOr<IReadOnlyList<RefreshToken>>>(error);
        }

        var revoked = new List<RefreshToken>();
        foreach (RefreshToken token in _tokens)
        {
            if (token.UserId != userId || token.RevokedAt is not null)
            {
                continue;
            }

            token.RevokedAt = revokedAt;
            revoked.Add(token);
        }

        return Task.FromResult<ErrorOr<IReadOnlyList<RefreshToken>>>(revoked);
    }

    public Task<ErrorOr<bool>> TryRevokeAndCreateAsync(
        Guid currentId,
        RefreshToken replacement,
        DateTime revokedAt,
        CancellationToken cancellationToken = default)
    {
        if (TryRevokeError is Error error)
        {
            return Task.FromResult<ErrorOr<bool>>(error);
        }

        if (!TryRevokeSucceeds)
        {
            return Task.FromResult<ErrorOr<bool>>(false);
        }

        RefreshToken? current = _tokens.Find(token => token.Id == currentId);
        if (current is not null)
        {
            current.RevokedAt = revokedAt;
        }

        _tokens.Add(replacement);
        return Task.FromResult<ErrorOr<bool>>(true);
    }

    public Task<ErrorOr<int>> PurgeExpiredAsync(
        DateTime utcNow,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<ErrorOr<int>>(0);
}
