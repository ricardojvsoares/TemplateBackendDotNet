using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Application.Abstractions.Messaging;
using Application.Abstractions.Storage;
using Application.Features.Files;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace WebApi.Tests;

[Collection(ApiSuite.Name)]
public sealed class FileEndpointTests
{
    private const string FilesRoute = "/api/v1/files";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ApiFixture _fixture;

    public FileEndpointTests(ApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task UploadWithoutAFileIsRejected()
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("one@example.com"), "Recipients");

        using HttpResponseMessage response = await _fixture.Client.PostAsync(FilesRoute, content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadStoresTheFilePublishesItsNameAndRecordsMail()
    {
        RecordingMailService.Clear();
        byte[] payload = [1, 2, 3];
        IMessageService messages = _fixture.Services.GetRequiredService<IMessageService>();
        var received = new TaskCompletionSource<FileUploadedMessage>(TaskCreationOptions.RunContinuationsAsynchronously);

        Func<FileUploadedMessage, CancellationToken, Task> handler = (message, _) =>
        {
            received.TrySetResult(message);
            return Task.CompletedTask;
        };

        await messages.SubscribeAsync(FileQueues.Uploaded, handler, CancellationToken.None);
        try
        {
            using var content = new MultipartFormDataContent();
            var file = new ByteArrayContent(payload);
            file.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
            content.Add(file, "File", "note.txt");
            content.Add(new StringContent("one@example.com"), "Recipients");

            using HttpResponseMessage response = await _fixture.Client.PostAsync(FilesRoute, content);
            string body = await response.Content.ReadAsStringAsync();
            Assert.True(response.StatusCode == HttpStatusCode.Accepted, body);

            StoredFile? stored = JsonSerializer.Deserialize<StoredFile>(body, JsonOptions);
            Assert.NotNull(stored);
            Assert.EndsWith("_note.txt", stored.FileName, StringComparison.Ordinal);

            Task completed = await Task.WhenAny(received.Task, Task.Delay(TimeSpan.FromSeconds(15)));
            Assert.Same(received.Task, completed);
            FileUploadedMessage message = await received.Task;
            Assert.Equal(stored.FileName, message.FileName);

            await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
            IStorageService storage = scope.ServiceProvider.GetRequiredService<IStorageService>();
            using MemoryStream downloaded = await storage.DownloadAsync(
                $"files/{stored.FileName}",
                CancellationToken.None);
            Assert.Equal(payload, downloaded.ToArray());

            RecordingMailService.RecordedMail mail = Assert.Single(RecordingMailService.Sent);
            Assert.Equal("one@example.com", Assert.Single(mail.Recipients));
            Assert.Equal("Test", mail.Subject);
            Assert.Equal(stored.FileName, Assert.Single(mail.AttachmentNames));
        }
        finally
        {
            await messages.UnsubscribeAsync(FileQueues.Uploaded, handler, CancellationToken.None);
        }
    }

    private sealed record StoredFile(string FileName);
}
