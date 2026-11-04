using Application.Abstractions.Messaging;

namespace Infrastructure.Services.Messaging;

internal sealed class MessageBroker : IMessageBroker
{
    public Task PublishAsync<T>(T message, Guid senderId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task SubscribeAsync<T>(Action<T> handler, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task UnsubscribeAsync<T>(Action<T> handler, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
