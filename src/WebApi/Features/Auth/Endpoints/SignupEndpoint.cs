using Application.Features.Auth;
using Application.Features.Auth.Commands.Signup;
using ErrorOr;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WebApi.Extensions;
using WebApi.RateLimiting;
using Wolverine;

namespace WebApi.Features.Auth.Endpoints;

internal static class SignupEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost(
            "/signup",
            async (
                [FromBody] SignupCommand command,
                [FromServices] IMessageBus bus,
                CancellationToken cancellationToken) =>
            {
                ErrorOr<AuthTokensResponse> result =
                    await bus.InvokeAsync<ErrorOr<AuthTokensResponse>>(command, cancellationToken);

                return result.ToCreatedResult();
            })
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitPolicies.Auth)
            .WithSummary("Sign Up")
            .WithName("Signup")
            .Produces<AuthTokensResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
