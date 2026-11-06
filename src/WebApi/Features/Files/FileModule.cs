using Carter;
using WebApi.Extensions;
using WebApi.Features.Files.Endpoints;

namespace WebApi.Features.Files;

public sealed class FileModule : ICarterModule
{
    private const string RoutePrefix = "files";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapApiGroup(RoutePrefix);

        UploadFileEndpoint.Map(group);
    }
}
