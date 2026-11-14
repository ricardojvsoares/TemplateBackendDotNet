using Application.Features.Users.Commands.SetUserAccess;
using Domain.Features.Users;
using ErrorOr;
using Microsoft.AspNetCore.Mvc;
using WebApi.Extensions;
using Wolverine;

namespace WebApi.Features.Users.Endpoints;

internal static class SetUserAccessEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPut(
            "/{id:guid}/access",
            async (
                Guid id,
                [FromBody] SetUserAccessRequest request,
                [FromServices] IMessageBus bus,
                CancellationToken cancellationToken) =>
            {
                ErrorOr<Success> result = await bus.InvokeAsync<ErrorOr<Success>>(
                    new SetUserAccessCommand(id, request.GlobalRole, request.Permissions),
                    cancellationToken);

                return result.ToNoContentResult();
            })
            .WithSummary("Set User Access")
            .WithName("SetUserAccess")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequirePermission(UserPolicies.Update);
    }
}

internal sealed class SetUserAccessRequest
{
    public string? GlobalRole { get; init; }

    public IReadOnlyList<string>? Permissions { get; init; }
}
