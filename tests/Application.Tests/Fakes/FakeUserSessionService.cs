using Application.Features.Auth;
using Domain.Features.Users;
using ErrorOr;

namespace Application.Tests.Fakes;

internal sealed class FakeUserSessionService : IUserSessionService
{
    public AuthTokensResponse Tokens { get; set; } = new(
        "access-token",
        "refresh-token",
        new DateTime(2026, 1, 2, 4, 0, 0, DateTimeKind.Utc));

    public Error? OpenError { get; set; }

    public Error? RotateError { get; set; }

    public Error? RevokeAllError { get; set; }

    public User? OpenedFor { get; private set; }

    public int OpenCalls { get; private set; }

    public string? RotatedToken { get; private set; }

    public Guid? RevokedAllUserId { get; private set; }

    public int RevokeAllCalls { get; private set; }

    public RevokeCurrentCall? RevokedCurrent { get; private set; }

    public Task<ErrorOr<AuthTokensResponse>> OpenAsync(
        User user,
        CancellationToken cancellationToken)
    {
        OpenCalls++;
        OpenedFor = user;
        if (OpenError is Error error)
        {
            return Task.FromResult<ErrorOr<AuthTokensResponse>>(error);
        }

        return Task.FromResult<ErrorOr<AuthTokensResponse>>(Tokens);
    }

    public Task<ErrorOr<AuthTokensResponse>> RotateAsync(
        string refreshToken,
        CancellationToken cancellationToken)
    {
        RotatedToken = refreshToken;
        if (RotateError is Error error)
        {
            return Task.FromResult<ErrorOr<AuthTokensResponse>>(error);
        }

        return Task.FromResult<ErrorOr<AuthTokensResponse>>(Tokens);
    }

    public Task<ErrorOr<Success>> RevokeCurrentAsync(
        Guid userId,
        string? refreshToken,
        string? accessJti,
        DateTime? accessExpiresAt,
        CancellationToken cancellationToken)
    {
        RevokedCurrent = new RevokeCurrentCall(userId, refreshToken, accessJti, accessExpiresAt);
        return Task.FromResult<ErrorOr<Success>>(Result.Success);
    }

    public Task<ErrorOr<Success>> RevokeAllAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        RevokeAllCalls++;
        RevokedAllUserId = userId;
        if (RevokeAllError is Error error)
        {
            return Task.FromResult<ErrorOr<Success>>(error);
        }

        return Task.FromResult<ErrorOr<Success>>(Result.Success);
    }

    internal sealed record RevokeCurrentCall(
        Guid UserId,
        string? RefreshToken,
        string? AccessJti,
        DateTime? AccessExpiresAt);
}
