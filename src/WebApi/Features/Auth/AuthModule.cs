using Carter;
using WebApi.Extensions;
using WebApi.Features.Auth.Endpoints;

namespace WebApi.Features.Auth;

public sealed class AuthModule : ICarterModule
{
    private const string RoutePrefix = "auth";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapApiGroup(RoutePrefix);

        SignupEndpoint.Map(group);
        LoginEndpoint.Map(group);
        RefreshTokenEndpoint.Map(group);
        LogoutEndpoint.Map(group);
        ChangePasswordEndpoint.Map(group);
    }
}
