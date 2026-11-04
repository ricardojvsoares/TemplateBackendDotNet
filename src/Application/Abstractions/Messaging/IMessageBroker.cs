namespace Application.Abstractions.Messaging;

public interface IMessageBroker
{
    Task PublishAsync<T>(
        T message, 
        Guid senderId, 
        CancellationToken cancellationToken);
    Task SubscribeAsync<T>(
        Action<T> handler, 
        CancellationToken cancellationToken);
    Task UnsubscribeAsync<T>(
        Action<T> handler, 
        CancellationToken cancellationToken);
}