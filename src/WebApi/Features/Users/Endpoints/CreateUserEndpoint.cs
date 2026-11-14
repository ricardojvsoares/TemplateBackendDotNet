using System.Text.Json.Serialization;
using Application.Features.Users.Commands.CreateUser;
using ErrorOr;
using Microsoft.AspNetCore.Mvc;
using WebApi.Extensions;
using WebApi.Json;
using Wolverine;
using Domain.Features.Users;

namespace WebApi.Features.Users.Endpoints;

internal static class CreateUserEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost(
            "/",
            async (
                [FromBody] CreateUserRequest request,
                [FromServices] IMessageBus bus,
                CancellationToken cancellationToken) =>
            {
                ErrorOr<CreateUserResponse> result =
                    await bus.InvokeAsync<ErrorOr<CreateUserResponse>>(
                        request.ToCommand(),
                        cancellationToken);

                return result.ToCreatedResult();
            })
            .WithSummary("Create User")
            .WithName("CreateUser")
            .Produces<CreateUserResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequirePermission(UserPolicies.Create);
    }
}

internal sealed class CreateUserRequest
{
    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    [JsonConverter(typeof(OptionalStringJsonConverter))]
    public OptionalString GlobalRole { get; init; }

    public IReadOnlyList<string>? Permissions { get; init; }

    public CreateUserCommand ToCommand() =>
        new(
            FirstName,
            LastName,
            Email,
            Password,
            GlobalRole.IsSpecified,
            GlobalRole.Value,
            Permissions);
}
