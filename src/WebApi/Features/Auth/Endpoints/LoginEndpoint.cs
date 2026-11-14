using Application.Features.Auth;
using Application.Features.Auth.Commands.Login;
using ErrorOr;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WebApi.Extensions;
using WebApi.RateLimiting;
using Wolverine;

namespace WebApi.Features.Auth.Endpoints;

internal static class LoginEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost(
            "/login",
            async (
                [FromBody] LoginCommand command,
                [FromServices] IMessageBus bus,
                CancellationToken cancellationToken) =>
            {
                ErrorOr<AuthTokensResponse> result =
                    await bus.InvokeAsync<ErrorOr<AuthTokensResponse>>(command, cancellationToken);

                return result.ToResult();
            })
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitPolicies.Auth)
            .WithSummary("Log In")
            .WithName("Login")
            .Produces<AuthTokensResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}
