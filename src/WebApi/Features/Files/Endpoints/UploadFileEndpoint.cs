using Application.Abstractions.Messaging;
using Application.Abstractions.Storage;
using Application.Features.Files;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Features.Files.Endpoints;

internal static class UploadFileEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost(
            "/",
            async (
                IFormFile file,
                [FromServices] IStorageService storage,
                [FromServices] IMessageService messages,
                CancellationToken cancellationToken) =>
            {
                if (file.Length == 0)
                {
                    return Results.BadRequest("File is empty.");
                }

                string fileName = Guid.CreateVersion7() + "_" + Path.GetFileName(file.FileName);
                string contentType = string.IsNullOrWhiteSpace(file.ContentType)
                    ? "application/octet-stream"
                    : file.ContentType;

                await using Stream content = file.OpenReadStream();
                await storage.StoreAsync(
                    content,
                    "files",
                    fileName,
                    contentType,
                    cancellationToken);

                var message = new FileUploadedMessage(fileName, $"files/{fileName}");

                await messages.PublishAsync(
                    FileQueues.Uploaded,
                    message,
                    Guid.CreateVersion7(),
                    cancellationToken);

                return Results.Accepted(value: message);
            })
            .DisableAntiforgery()
            .AllowAnonymous()
            .WithSummary("Upload file (mock)")
            .WithName("UploadFile")
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<FileUploadedMessage>(StatusCodes.Status202Accepted)
            .Produces(StatusCodes.Status400BadRequest);
    }
}
