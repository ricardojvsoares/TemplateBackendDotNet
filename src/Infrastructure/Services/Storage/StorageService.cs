using Amazon.S3;
using Amazon.S3.Model;
using Application.Abstractions.Storage;
using Application.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services.Storage;

public sealed class StorageService(
    ILogger<StorageService> logger,
    IAmazonS3 s3,
    IOptions<StorageServiceSettings> options)
    : IStorageService
{
    private readonly StorageServiceSettings _settings = options.Value;

    public async Task<MemoryStream> DownloadAsync(
        string path,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await s3.GetObjectAsync(
                _settings.Bucket,
                path,
                cancellationToken);

            var memoryStream = new MemoryStream();
            await response.ResponseStream.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;
            return memoryStream;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error downloading object '{ObjectKey}' from bucket '{Bucket}'",
                path,
                _settings.Bucket);
            throw;
        }
    }

    public async Task StoreAsync(
        Stream content,
        string path,
        string fileName,
        string contentType,
        CancellationToken cancellationToken)
    {
        var objectKey = BuildObjectKey(path, fileName);
        FileStream? tempFile = null;

        try
        {
            var uploadStream = content;

            if (!content.CanSeek)
            {
                var tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                tempFile = new FileStream(
                    tempPath,
                    FileMode.CreateNew,
                    FileAccess.ReadWrite,
                    FileShare.None,
                    bufferSize: 81920,
                    options: FileOptions.Asynchronous | FileOptions.DeleteOnClose);

                await content.CopyToAsync(tempFile, cancellationToken);
                await tempFile.FlushAsync(cancellationToken);
                tempFile.Position = 0;
                uploadStream = tempFile;
            }
            else
            {
                content.Position = 0;
            }

            var request = new PutObjectRequest
            {
                BucketName = _settings.Bucket,
                Key = objectKey,
                InputStream = uploadStream,
                ContentType = contentType,
                AutoCloseStream = false,
                UseChunkEncoding = false,
            };

            request.Headers.ContentLength = uploadStream.Length;

            if (_settings.DisablePayloadSigning
                && _settings.Endpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                request.DisablePayloadSigning = true;
            }

            await s3.PutObjectAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error uploading object '{ObjectKey}' to bucket '{Bucket}'",
                objectKey,
                _settings.Bucket);
            throw;
        }
        finally
        {
            if (tempFile is not null)
            {
                await tempFile.DisposeAsync();
            }
        }
    }

    public async Task<bool> ExistsAsync(
        string path,
        CancellationToken cancellationToken)
    {
        try
        {
            await s3.GetObjectMetadataAsync(
                _settings.Bucket,
                path,
                cancellationToken);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Error checking object '{ObjectKey}' in bucket '{Bucket}'",
                path,
                _settings.Bucket);
            return false;
        }
    }

    private static string BuildObjectKey(string path, string fileName)
    {
        var normalizedPath = path.Replace('\\', '/').Trim('/');
        return string.IsNullOrEmpty(normalizedPath)
            ? fileName
            : $"{normalizedPath}/{fileName}";
    }
}
