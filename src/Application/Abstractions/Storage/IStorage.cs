namespace Application.Abstractions.Storage;

public interface IStorage
{
    Task<MemoryStream> DownloadAsync(
        string path,
        CancellationToken cancellationToken);
    Task StoreAsync(
        Stream content,
        string path,
        string fileName,
        string contentType,
        CancellationToken cancellationToken);
    Task<bool> ExistsAsync(
        string path,
        CancellationToken cancellationToken);
}