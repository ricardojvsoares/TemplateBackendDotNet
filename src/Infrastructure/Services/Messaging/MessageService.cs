using System.Collections.Concurrent;
using System.Text.Json;
using Application.Abstractions.Messaging;
using Application.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Infrastructure.Services.Messaging;

public sealed class MessageService(
    IOptions<MessageServiceSettings> options,
    ILogger<MessageService> logger)
    : IMessageService, IAsyncDisposable
{
    internal const string SenderIdHeader = "sender-id";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private readonly SemaphoreSlim _publishLock = new(1, 1);
    private readonly ConcurrentDictionary<(string Queue, Type MessageType, Delegate Handler), Subscription> _subscriptions = new();

    private IConnection? _connection;
    private IChannel? _publishChannel;

    public async Task PublishAsync<T>(
        string queue,
        T message,
        Guid senderId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queue);
        ArgumentNullException.ThrowIfNull(message);

        byte[] body = JsonSerializer.SerializeToUtf8Bytes(message, JsonOptions);

        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            MessageId = Guid.NewGuid().ToString(),
            Headers = new Dictionary<string, object?> { [SenderIdHeader] = senderId.ToString() },
        };

        await _publishLock.WaitAsync(cancellationToken);
        try
        {
            IChannel channel = await GetPublishChannelAsync(cancellationToken);

            await channel.QueueDeclareAsync(
                queue: queue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: cancellationToken);

            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: queue,
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);

            logger.LogInformation(
                "Published {MessageType} to RabbitMQ queue {Queue}",
                typeof(T).Name,
                queue);
        }
        finally
        {
            _publishLock.Release();
        }
    }

    public async Task SubscribeAsync<T>(
        string queue,
        Func<T, CancellationToken, Task> handler,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queue);
        ArgumentNullException.ThrowIfNull(handler);

        var key = (queue, typeof(T), (Delegate)handler);
        if (_subscriptions.ContainsKey(key))
        {
            return;
        }

        IConnection connection = await GetConnectionAsync(cancellationToken);
        IChannel channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
        var subscription = new Subscription(channel);

        try
        {
            await channel.QueueDeclareAsync(
                queue: queue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: cancellationToken);

            await channel.BasicQosAsync(0, 10, false, cancellationToken);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += (_, args) => HandleDeliveryAsync(channel, args, handler, subscription.Cts.Token);

            subscription.ConsumerTag = await channel.BasicConsumeAsync(
                queue,
                autoAck: false,
                consumer: consumer,
                cancellationToken: cancellationToken);
        }
        catch
        {
            await subscription.DisposeAsync();
            throw;
        }

        if (!_subscriptions.TryAdd(key, subscription))
        {
            await subscription.DisposeAsync();
        }
    }

    public async Task UnsubscribeAsync<T>(
        string queue,
        Func<T, CancellationToken, Task> handler,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queue);
        ArgumentNullException.ThrowIfNull(handler);

        if (!_subscriptions.TryRemove((queue, typeof(T), handler), out Subscription? subscription))
        {
            return;
        }

        await subscription.CancelAsync(cancellationToken);
        await subscription.DisposeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        foreach (Subscription subscription in _subscriptions.Values)
        {
            await subscription.DisposeAsync();
        }

        _subscriptions.Clear();

        if (_publishChannel is not null)
        {
            await _publishChannel.DisposeAsync();
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        _connectionLock.Dispose();
        _publishLock.Dispose();
    }

    private async Task HandleDeliveryAsync<T>(
        IChannel channel,
        BasicDeliverEventArgs args,
        Func<T, CancellationToken, Task> handler,
        CancellationToken cancellationToken)
    {
        try
        {
            T? message = JsonSerializer.Deserialize<T>(args.Body.Span, JsonOptions)
                ?? throw new JsonException($"Message {args.BasicProperties.MessageId} deserialized to null.");

            await handler(message, cancellationToken);
            await channel.BasicAckAsync(args.DeliveryTag, multiple: false, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Handler for {MessageType} failed (message id {MessageId}); message dropped",
                typeof(T).Name,
                args.BasicProperties.MessageId);

            await channel.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: false, CancellationToken.None);
        }
    }

    private async Task<IChannel> GetPublishChannelAsync(CancellationToken cancellationToken)
    {
        if (_publishChannel is { IsOpen: true })
        {
            return _publishChannel;
        }

        IConnection connection = await GetConnectionAsync(cancellationToken);
        _publishChannel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
        return _publishChannel;
    }

    private async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection is { IsOpen: true })
        {
            return _connection;
        }

        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (_connection is { IsOpen: true })
            {
                return _connection;
            }

            MessageServiceSettings settings = options.Value;
            var factory = new ConnectionFactory
            {
                HostName = settings.Host,
                Port = settings.Port,
                UserName = settings.Username,
                Password = settings.Password,
                VirtualHost = "/",
                AutomaticRecoveryEnabled = true,
            };

            _connection = await factory.CreateConnectionAsync(cancellationToken);
            return _connection;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    private sealed class Subscription(IChannel channel) : IAsyncDisposable
    {
        internal CancellationTokenSource Cts { get; } = new();
        internal string? ConsumerTag { get; set; }

        internal async Task CancelAsync(CancellationToken cancellationToken)
        {
            await Cts.CancelAsync();

            if (ConsumerTag is not null && channel.IsOpen)
            {
                await channel.BasicCancelAsync(ConsumerTag, cancellationToken: cancellationToken);
            }
        }

        public async ValueTask DisposeAsync()
        {
            Cts.Dispose();
            await channel.DisposeAsync();
        }
    }
}
