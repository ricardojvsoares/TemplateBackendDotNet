using System.Text;
using Application.Abstractions.Messaging;
using Application.Abstractions.Storage;
using Application.Features.Files;

namespace FileProcessor;

public sealed class FileProcessorWorker(
    IMessageService messages,
    IServiceScopeFactory scopeFactory,
    ILogger<FileProcessorWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await messages.SubscribeAsync<FileUploadedMessage>(
            FileQueues.Uploaded,
            HandleAsync,
            stoppingToken);

        logger.LogInformation("Listening on queue '{Queue}'", FileQueues.Uploaded);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await messages.UnsubscribeAsync<FileUploadedMessage>(
            FileQueues.Uploaded,
            HandleAsync,
            cancellationToken);

        await base.StopAsync(cancellationToken);
    }

    private async Task HandleAsync(FileUploadedMessage message, CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        IStorageService storage = scope.ServiceProvider.GetRequiredService<IStorageService>();

        using MemoryStream stream = await storage.DownloadAsync(message.ObjectKey, cancellationToken);
        string content = Encoding.UTF8.GetString(stream.ToArray());

        Console.WriteLine($"----- {message.FileName} -----");
        Console.WriteLine(content);
        Console.WriteLine($"----- end of {message.FileName} -----");
    }
}
