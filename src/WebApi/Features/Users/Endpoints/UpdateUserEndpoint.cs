using Application.Features.Users.Commands.UpdateUser;
using Domain.Features.Users;
using ErrorOr;
using Microsoft.AspNetCore.Mvc;
using WebApi.Extensions;
using Wolverine;

namespace WebApi.Features.Users.Endpoints;

internal static class UpdateUserEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPut(
            "/{id:guid}",
            async (
                Guid id,
                [FromBody] UpdateUserRequest request,
                [FromServices] IMessageBus bus,
                CancellationToken cancellationToken) =>
            {
                ErrorOr<Success> result = await bus.InvokeAsync<ErrorOr<Success>>(
                    new UpdateUserCommand(id, request.FirstName, request.LastName),
                    cancellationToken);

                return result.ToNoContentResult();
            })
            .WithSummary("Update User")
            .WithName("UpdateUser")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequirePermission(UserPolicies.Update);
    }
}

internal sealed record UpdateUserRequest(string FirstName, string LastName);
