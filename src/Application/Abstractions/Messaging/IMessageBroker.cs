namespace Application.Abstractions.Messaging;

public interface IMessageBroker
{
    Task PublishAsync<T>(
        T message,
        Guid senderId,
        CancellationToken cancellationToken);
    Task SubscribeAsync<T>(
        Func<T, CancellationToken, Task> handler,
        CancellationToken cancellationToken);
    Task UnsubscribeAsync<T>(
        Func<T, CancellationToken, Task> handler,
        CancellationToken cancellationToken);
}