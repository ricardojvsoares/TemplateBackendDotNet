namespace Application.Abstractions.Jobs;

public interface IRecurringJob
{
    string Name { get; }

    string DefaultCron { get; }

    Task ExecuteAsync(CancellationToken cancellationToken);
}
