using Application.Features.Auth.Commands.Logout;
using ErrorOr;
using Microsoft.AspNetCore.Mvc;
using WebApi.Extensions;
using Wolverine;

namespace WebApi.Features.Auth.Endpoints;

internal static class LogoutEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost(
            "/logout",
            async (
                [FromBody] LogoutCommand command,
                [FromServices] IMessageBus bus,
                CancellationToken cancellationToken) =>
            {
                ErrorOr<Success> result =
                    await bus.InvokeAsync<ErrorOr<Success>>(command, cancellationToken);

                return result.ToNoContentResult();
            })
            .RequireAuthorization()
            .WithSummary("Log Out")
            .WithName("Logout")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}
