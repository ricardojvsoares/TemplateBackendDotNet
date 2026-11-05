using Carter;
using WebApi.Extensions;
using WebApi.Features.Users.Endpoints;

namespace WebApi.Features.Users;

public sealed class UserModule : ICarterModule
{
    private const string RoutePrefix = "users";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapApiGroup(RoutePrefix);

        CreateUserEndpoint.Map(group);
    }
}
