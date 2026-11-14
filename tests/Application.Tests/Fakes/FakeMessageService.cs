using Application.Abstractions.Messaging;

namespace Application.Tests.Fakes;

internal sealed class FakeMessageService : IMessageService
{
    private readonly List<PublishedMessage> _published = [];

    public IReadOnlyList<PublishedMessage> Published => _published;

    public Task PublishAsync<T>(
        string queue,
        T message,
        Guid senderId,
        CancellationToken cancellationToken)
    {
        _published.Add(new PublishedMessage(queue, message, senderId));
        return Task.CompletedTask;
    }

    public Task SubscribeAsync<T>(
        string queue,
        Func<T, CancellationToken, Task> handler,
        CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task UnsubscribeAsync<T>(
        string queue,
        Func<T, CancellationToken, Task> handler,
        CancellationToken cancellationToken) =>
        Task.CompletedTask;

    internal sealed record PublishedMessage(string Queue, object? Message, Guid SenderId);
}
