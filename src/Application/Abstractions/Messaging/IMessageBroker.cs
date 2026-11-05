namespace Application.Abstractions.Messaging;

public interface IMessageBroker
{
    Task PublishAsync<T>(
        T message,
        Guid senderId,
        CancellationToken cancellationToken);
}