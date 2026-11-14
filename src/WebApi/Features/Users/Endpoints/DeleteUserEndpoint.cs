using Application.Features.Users.Commands.DeleteUser;
using Domain.Features.Users;
using ErrorOr;
using Microsoft.AspNetCore.Mvc;
using WebApi.Extensions;
using Wolverine;

namespace WebApi.Features.Users.Endpoints;

internal static class DeleteUserEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapDelete(
            "/{id:guid}",
            async (
                Guid id,
                [FromServices] IMessageBus bus,
                CancellationToken cancellationToken) =>
            {
                ErrorOr<Success> result = await bus.InvokeAsync<ErrorOr<Success>>(
                    new DeleteUserCommand(id),
                    cancellationToken);

                return result.ToNoContentResult();
            })
            .WithSummary("Delete User")
            .WithName("DeleteUser")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequirePermission(UserPolicies.Delete);
    }
}
