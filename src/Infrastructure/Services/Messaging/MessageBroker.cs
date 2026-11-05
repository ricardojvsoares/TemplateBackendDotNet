using Application.Abstractions.Messaging;
using Wolverine;

namespace Infrastructure.Services.Messaging;

internal sealed class MessageBroker(IMessageBus bus) : IMessageBroker
{
    internal const string SenderIdHeader = "sender-id";

    public async Task PublishAsync<T>(T message, Guid senderId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        cancellationToken.ThrowIfCancellationRequested();

        var options = new DeliveryOptions();
        options.Headers[SenderIdHeader] = senderId.ToString();

        await bus.PublishAsync(message, options);
    }
}
