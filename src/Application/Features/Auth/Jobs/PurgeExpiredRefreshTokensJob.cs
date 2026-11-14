using Application.Abstractions.Identity;
using Application.Abstractions.Jobs;
using ErrorOr;
using Microsoft.Extensions.Logging;

namespace Application.Features.Auth.Jobs;

public sealed class PurgeExpiredRefreshTokensJob(
    IRefreshTokenStore refreshTokens,
    TimeProvider time,
    ILogger<PurgeExpiredRefreshTokensJob> logger) : IRecurringJob
{
    public string Name => "purge-expired-refresh-tokens";

    public string DefaultCron => "0 * * * *";

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        ErrorOr<int> deleted = await refreshTokens.PurgeExpiredAsync(
            time.GetUtcNow().UtcDateTime,
            cancellationToken);

        if (deleted.IsError)
        {
            throw new InvalidOperationException(deleted.FirstError.Description);
        }

        logger.LogInformation("Purged {Count} expired refresh tokens", deleted.Value);
    }
}
