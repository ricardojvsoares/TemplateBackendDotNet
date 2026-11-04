using ErrorOr;
using Application.Features.Users.Commands.CreateUser;
using WebApi.Extensions;
using Microsoft.AspNetCore.Mvc;
using Wolverine;

namespace WebApi.Features.Users.Endpoints;

internal static class CreateUserEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost(
            "/",
            async (
                [FromBody] CreateUserCommand command,
                [FromServices] IMessageBus bus,
                CancellationToken cancellationToken) =>
            {
                ErrorOr<CreateUserResponse> result =
                    await bus.InvokeAsync<ErrorOr<CreateUserResponse>>(
                        command,
                        cancellationToken);

                if (result.IsError)
                {
                    return result.ToResult();
                }

                return Results.Ok(result.Value);
            })
            .WithSummary("Create User")
            .WithName("CreateUser")
            .Produces<CreateUserResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem();
    }
}
