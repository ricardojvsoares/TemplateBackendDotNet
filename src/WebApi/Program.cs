using Asp.Versioning;
using Infrastructure;
using Persistence;
using Application;
using Serilog;
using Scalar.AspNetCore;
using Carter;

var builder = WebApplication.CreateBuilder(args);


builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = ApiVersion.Default;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
        options.AssumeDefaultVersionWhenUnspecified = true;
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    })
    .AddOpenApi();



builder.Host.UseSerilog(
    (context, configuration) =>
        configuration.ReadFrom.Configuration(
            context.Configuration));

builder.Host.UseInfrastructureMessaging();

builder.Services
    .AddApplication()
    .AddInfrastructure()
    .AddPersistence();

builder.Services.AddCarter();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().WithDocumentPerVersion();

    app.MapScalarApiReference("/docs", options =>
    {
        options.WithTitle("Api");
        options.WithOpenApiRoutePattern("/openapi/{documentName}.json");
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapCarter();

await app.RunAsync();