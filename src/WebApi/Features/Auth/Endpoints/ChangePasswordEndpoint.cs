using Application.Features.Auth.Commands.ChangePassword;
using ErrorOr;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WebApi.Extensions;
using WebApi.RateLimiting;
using Wolverine;

namespace WebApi.Features.Auth.Endpoints;

internal static class ChangePasswordEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost(
            "/change-password",
            async (
                [FromBody] ChangePasswordCommand command,
                [FromServices] IMessageBus bus,
                CancellationToken cancellationToken) =>
            {
                ErrorOr<Success> result =
                    await bus.InvokeAsync<ErrorOr<Success>>(command, cancellationToken);

                return result.ToNoContentResult();
            })
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitPolicies.Auth)
            .WithSummary("Change Password")
            .WithName("ChangePassword")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}
