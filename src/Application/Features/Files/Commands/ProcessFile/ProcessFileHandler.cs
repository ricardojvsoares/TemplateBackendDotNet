using Application.Abstractions.Mail;
using Application.Abstractions.Messaging;
using Application.Abstractions.Storage;
using ErrorOr;
using Microsoft.Extensions.Logging;

namespace Application.Features.Files.Commands.ProcessFile;

public static class ProcessFileHandler
{
    public static async Task<ErrorOr<ProcessFileResponse>> HandleAsync(
        ProcessFileCommand command,
        IStorageService storage,
        IMessageService messages,
        IMailService mail,
        ILogger<ProcessFileCommand> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(command.FileName))
            {
                return Error.Validation("File.NameEmpty", "File name is empty");
            }

            using var buffer = new MemoryStream();
            await command.FileStream.CopyToAsync(buffer, cancellationToken);
            byte[] fileBytes = buffer.ToArray();

            using var storageStream = new MemoryStream(fileBytes);

            await storage.StoreAsync(
                storageStream,
                "files",
                command.FileName,
                command.ContentType,
                cancellationToken);

            var message = new FileUploadedMessage(command.FileName, $"files/{command.FileName}");

            await messages.PublishAsync(
                FileQueues.Uploaded,
                message,
                Guid.CreateVersion7(),
                cancellationToken);

            if (command.Recipients.Count > 0 && !command.Recipients.Any(string.IsNullOrWhiteSpace))
            {
                var attachment = new EmailAttachment
                {
                    FileName = command.FileName,
                    ContentType = command.ContentType,
                    Content = fileBytes
                };

                await mail.SendAsync(
                    command.Recipients,
                    cc: [],
                    "Test",
                    "random body",
                    [attachment],
                    cancellationToken);
            }

            return new ProcessFileResponse(command.FileName);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing file {FileName}", command.FileName);
            return Error.Unexpected("File.ProcessError", "Error processing file");
        }

    }
}