namespace Application.Abstractions.Messaging;

public interface IMessageService
{
    Task PublishAsync<T>(
        string queue,
        T message,
        Guid senderId,
        CancellationToken cancellationToken);

    Task SubscribeAsync<T>(
        string queue,
        Func<T, CancellationToken, Task> handler,
        CancellationToken cancellationToken);

    Task UnsubscribeAsync<T>(
        string queue,
        Func<T, CancellationToken, Task> handler,
        CancellationToken cancellationToken);
}
