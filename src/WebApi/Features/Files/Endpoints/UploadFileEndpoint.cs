using Application.Features.Files.Commands.ProcessFile;
using ErrorOr;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WebApi.Extensions;
using WebApi.RateLimiting;
using Wolverine;

namespace WebApi.Features.Files.Endpoints;

internal static class UploadFileEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost(
            "/",
            async (
                [FromForm] UploadFileRequest request,
                [FromServices] IMessageBus bus,
                CancellationToken cancellationToken) =>
            {
                if (request.File is null)
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["File.Missing"] = ["File is required."]
                    });
                }

                string fileName = Guid.CreateVersion7() + "_" + Path.GetFileName(request.File.FileName);
                string contentType = string.IsNullOrWhiteSpace(request.File.ContentType)
                    ? "application/octet-stream"
                    : request.File.ContentType;

                await using var stream = request.File.OpenReadStream();

                ProcessFileCommand command = request.ToCommand(stream, fileName, contentType);

                ErrorOr<ProcessFileResponse> result =
                    await bus.InvokeAsync<ErrorOr<ProcessFileResponse>>(
                        command,
                        cancellationToken);

                return result.ToAcceptedResult();
            })
            .DisableAntiforgery()
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitPolicies.Upload)
            .WithSummary("Upload file (mock)")
            .WithName("UploadFile")
            .Produces<ProcessFileResponse>(StatusCodes.Status202Accepted)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
}

internal sealed record UploadFileRequest(
    List<string> Recipients,
    IFormFile File)
{
    public ProcessFileCommand ToCommand(Stream stream, string fileName, string contentType) =>
        new(Recipients, stream, fileName, contentType);
}