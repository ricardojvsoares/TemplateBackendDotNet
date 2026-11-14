using Application.Abstractions.Mail;
using Application.Features.Files;
using Application.Features.Files.Commands.ProcessFile;
using Application.Tests.Fakes;
using ErrorOr;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Application.Tests;

public sealed class ProcessFileHandlerTests
{
    [Fact]
    public async Task ProcessFileRejectsAnEmptyName()
    {
        var storage = new FakeStorageService();
        var messages = new FakeMessageService();
        var mail = new FakeMailService();
        using var stream = new MemoryStream([1, 2, 3]);

        ErrorOr<ProcessFileResponse> result = await ProcessFileHandler.HandleAsync(
            new ProcessFileCommand([], stream, "  ", "text/plain"),
            storage,
            messages,
            mail,
            NullLogger<ProcessFileCommand>.Instance,
            CancellationToken.None);

        ErrorAssert.Code(result, "File.NameEmpty");
        Assert.Empty(storage.Stored);
        Assert.Empty(messages.Published);
        Assert.Empty(mail.Sent);
    }

    [Fact]
    public async Task ProcessFileStoresAndPublishesWithoutMail()
    {
        var storage = new FakeStorageService();
        var messages = new FakeMessageService();
        var mail = new FakeMailService();
        using var stream = new MemoryStream([1, 2, 3]);

        ErrorOr<ProcessFileResponse> result = await ProcessFileHandler.HandleAsync(
            new ProcessFileCommand([], stream, "note.txt", "text/plain"),
            storage,
            messages,
            mail,
            NullLogger<ProcessFileCommand>.Instance,
            CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal("note.txt", result.Value.FileName);
        FakeStorageService.StoredFile stored = Assert.Single(storage.Stored);
        Assert.Equal("files", stored.Path);
        Assert.Equal("note.txt", stored.FileName);
        Assert.Equal("text/plain", stored.ContentType);
        Assert.Equal(new byte[] { 1, 2, 3 }, stored.Content);
        FakeMessageService.PublishedMessage published = Assert.Single(messages.Published);
        Assert.Equal(FileQueues.Uploaded, published.Queue);
        FileUploadedMessage message = Assert.IsType<FileUploadedMessage>(published.Message);
        Assert.Equal("note.txt", message.FileName);
        Assert.Equal("files/note.txt", message.ObjectKey);
        Assert.NotEqual(Guid.Empty, published.SenderId);
        Assert.Empty(mail.Sent);
    }

    [Fact]
    public async Task ProcessFileSendsMailWhenRecipientsExist()
    {
        var storage = new FakeStorageService();
        var messages = new FakeMessageService();
        var mail = new FakeMailService();
        using var stream = new MemoryStream([1, 2, 3]);

        ErrorOr<ProcessFileResponse> result = await ProcessFileHandler.HandleAsync(
            new ProcessFileCommand(["one@example.com"], stream, "note.txt", "text/plain"),
            storage,
            messages,
            mail,
            NullLogger<ProcessFileCommand>.Instance,
            CancellationToken.None);

        Assert.False(result.IsError);
        FakeMailService.SentMail sent = Assert.Single(mail.Sent);
        Assert.Equal(["one@example.com"], sent.Recipients);
        EmailAttachment attachment = Assert.Single(sent.Attachments!);
        Assert.Equal("note.txt", attachment.FileName);
        Assert.Equal("text/plain", attachment.ContentType);
        Assert.Equal(new byte[] { 1, 2, 3 }, attachment.Content.ToArray());
        Assert.Single(storage.Stored);
        Assert.Single(messages.Published);
    }

    [Fact]
    public async Task ProcessFileReturnsAProcessErrorWhenStorageFails()
    {
        var storage = new FakeStorageService { StoreError = new InvalidOperationException("disk") };
        var messages = new FakeMessageService();
        var mail = new FakeMailService();
        using var stream = new MemoryStream([1, 2, 3]);

        ErrorOr<ProcessFileResponse> result = await ProcessFileHandler.HandleAsync(
            new ProcessFileCommand(["one@example.com"], stream, "note.txt", "text/plain"),
            storage,
            messages,
            mail,
            NullLogger<ProcessFileCommand>.Instance,
            CancellationToken.None);

        ErrorAssert.Code(result, "File.ProcessError");
        Assert.Empty(messages.Published);
        Assert.Empty(mail.Sent);
    }

    [Fact]
    public async Task ProcessFileRethrowsCancellation()
    {
        var storage = new FakeStorageService { StoreError = new OperationCanceledException() };
        using var stream = new MemoryStream([1, 2, 3]);

        await Assert.ThrowsAsync<OperationCanceledException>(() => ProcessFileHandler.HandleAsync(
            new ProcessFileCommand([], stream, "note.txt", "text/plain"),
            storage,
            new FakeMessageService(),
            new FakeMailService(),
            NullLogger<ProcessFileCommand>.Instance,
            CancellationToken.None));
    }
}
