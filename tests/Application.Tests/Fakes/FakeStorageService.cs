using Application.Abstractions.Storage;

namespace Application.Tests.Fakes;

internal sealed class FakeStorageService : IStorageService
{
    private readonly List<StoredFile> _stored = [];

    public IReadOnlyList<StoredFile> Stored => _stored;

    public Exception? StoreError { get; set; }

    public Task<MemoryStream> DownloadAsync(string path, CancellationToken cancellationToken) =>
        Task.FromResult(new MemoryStream());

    public async Task StoreAsync(
        Stream content,
        string path,
        string fileName,
        string contentType,
        CancellationToken cancellationToken)
    {
        if (StoreError is not null)
        {
            throw StoreError;
        }

        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        _stored.Add(new StoredFile(path, fileName, contentType, buffer.ToArray()));
    }

    public Task<bool> ExistsAsync(string path, CancellationToken cancellationToken) =>
        Task.FromResult(false);

    internal sealed record StoredFile(string Path, string FileName, string ContentType, byte[] Content);
}
