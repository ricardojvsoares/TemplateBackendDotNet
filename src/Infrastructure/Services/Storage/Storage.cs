using Application.Abstractions.Storage;

namespace Infrastructure.Services.Storage;

public class Storage : IStorage
{
    public Task<MemoryStream> DownloadAsync(string path, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ExistsAsync(string path, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task StoreAsync(Stream content, string path, string fileName, string contentType, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}