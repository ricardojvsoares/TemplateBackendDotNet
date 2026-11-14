using Application.Abstractions.Identity;
using Asp.Versioning;
using Infrastructure;
using Infrastructure.Telemetry;
using Persistence;
using Application;
using Serilog;
using Scalar.AspNetCore;
using Carter;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using WebApi.Extensions;

DotNetEnv.Env.NoClobber().TraversePath().Load();
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
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .WriteToOpenTelemetry(context.Configuration, "WebApi"));

builder.Host.UseInfrastructureMessaging();

builder.Services
    .AddAppTelemetry(builder.Configuration, "WebApi", instrumentAspNetCore: true)
    .AddApplication()
    .AddInfrastructure()
    .AddPersistence()
    .AddApiRateLimiting();

builder.Services.AddCarter();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().WithDocumentPerVersion().DisableRateLimiting();

    app.MapScalarApiReference("/docs", options =>
    {
        options.WithTitle("Api");
        options.WithOpenApiRoutePattern("/openapi/{documentName}.json");
    }).DisableRateLimiting();
}

if (!string.IsNullOrEmpty(builder.Configuration["ASPNETCORE_HTTPS_PORTS"])
    || !string.IsNullOrEmpty(builder.Configuration["HTTPS_PORT"]))
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapCarter();

await using (AsyncServiceScope scope = app.Services.CreateAsyncScope())
{
    IAdminSeeder seeder = scope.ServiceProvider.GetRequiredService<IAdminSeeder>();
    await seeder.SeedAsync();
}

await app.RunAsync();

public partial class Program
{
    protected Program()
    {
    }
}