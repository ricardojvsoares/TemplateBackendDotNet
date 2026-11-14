using Application.Features.Users.Commands.UpdateUserPassword;
using Domain.Features.Users;
using ErrorOr;
using Microsoft.AspNetCore.Mvc;
using WebApi.Extensions;
using Wolverine;

namespace WebApi.Features.Users.Endpoints;

internal static class UpdateUserPasswordEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPut(
            "/{id:guid}/password",
            async (
                Guid id,
                [FromBody] UpdateUserPasswordRequest request,
                [FromServices] IMessageBus bus,
                CancellationToken cancellationToken) =>
            {
                ErrorOr<Success> result = await bus.InvokeAsync<ErrorOr<Success>>(
                    new UpdateUserPasswordCommand(id, request.Password),
                    cancellationToken);

                return result.ToNoContentResult();
            })
            .WithSummary("Set User Password")
            .WithName("UpdateUserPassword")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequirePermission(UserPolicies.Update);
    }
}

internal sealed record UpdateUserPasswordRequest(string Password);
