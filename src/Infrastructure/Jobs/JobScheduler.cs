using System.Globalization;
using Application.Abstractions.Cache;
using Application.Abstractions.Jobs;
using Cronos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Jobs;

internal sealed class JobScheduler(
    IServiceScopeFactory scopeFactory,
    ICacheService cache,
    IConfiguration configuration,
    TimeProvider time,
    ILogger<JobScheduler> logger) : BackgroundService
{
    private readonly List<ScheduledJob> _jobs = [];

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _jobs.AddRange(await DiscoverAsync());
        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_jobs.Count == 0)
        {
            logger.LogInformation("No recurring jobs registered");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            DateTimeOffset now = time.GetUtcNow();
            ScheduledJob? due = null;
            DateTimeOffset dueAt = DateTimeOffset.MaxValue;

            foreach (ScheduledJob job in _jobs)
            {
                DateTimeOffset? next = job.Expression.GetNextOccurrence(now, TimeZoneInfo.Utc);
                if (next is null || next.Value >= dueAt)
                {
                    continue;
                }

                due = job;
                dueAt = next.Value;
            }

            if (due is null)
            {
                await Task.Delay(TimeSpan.FromMinutes(1), time, stoppingToken);
                continue;
            }

            TimeSpan delay = dueAt - time.GetUtcNow();
            if (delay > TimeSpan.FromDays(1))
            {
                await Task.Delay(TimeSpan.FromDays(1), time, stoppingToken);
                continue;
            }

            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, time, stoppingToken);
            }

            await RunAsync(due, dueAt, stoppingToken);
        }
    }

    private async Task<List<ScheduledJob>> DiscoverAsync()
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        var jobs = new List<ScheduledJob>();
        foreach (IRecurringJob job in scope.ServiceProvider.GetServices<IRecurringJob>())
        {
            string variable = "JOB_" + job.Name.ToUpperInvariant().Replace('-', '_') + "_CRON";
            string? configured = configuration[variable];
            string cron = string.IsNullOrWhiteSpace(configured) ? job.DefaultCron : configured.Trim();
            CronExpression expression = CronExpression.Parse(cron);
            jobs.Add(new ScheduledJob(job.Name, expression));
            logger.LogInformation("Scheduled job {Job} with cron '{Cron}'", job.Name, cron);
        }

        return jobs;
    }

    private async Task RunAsync(
        ScheduledJob job,
        DateTimeOffset occurrence,
        CancellationToken cancellationToken)
    {
        try
        {
            string occurrenceKey = occurrence.UtcDateTime.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            TimeSpan ttl = LockTtl(job.Expression, occurrence);
            bool acquired = await cache.TryAddAsync(
                JobCacheKeys.Occurrence(job.Name, occurrenceKey),
                "1",
                ttl,
                cancellationToken);

            if (!acquired)
            {
                logger.LogInformation(
                    "Skipping job {Job} occurrence {Occurrence}; another instance owns it",
                    job.Name,
                    occurrenceKey);
                return;
            }

            await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
            IRecurringJob? recurring = null;
            foreach (IRecurringJob candidate in scope.ServiceProvider.GetServices<IRecurringJob>())
            {
                if (candidate.Name == job.Name)
                {
                    recurring = candidate;
                    break;
                }
            }

            if (recurring is null)
            {
                logger.LogError("Job {Job} is no longer registered", job.Name);
                return;
            }

            logger.LogInformation("Running job {Job}", job.Name);
            await recurring.ExecuteAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Job {Job} failed", job.Name);
        }
    }

    private static TimeSpan LockTtl(CronExpression expression, DateTimeOffset occurrence)
    {
        DateTimeOffset? following = expression.GetNextOccurrence(occurrence, TimeZoneInfo.Utc);
        if (following is null)
        {
            return TimeSpan.FromHours(2);
        }

        TimeSpan untilNext = following.Value - occurrence;
        return untilNext > TimeSpan.FromMinutes(1) ? untilNext : TimeSpan.FromMinutes(1);
    }

    private sealed record ScheduledJob(string Name, CronExpression Expression);
}
