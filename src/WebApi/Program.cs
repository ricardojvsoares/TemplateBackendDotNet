using Application.Abstractions.Identity;
using Asp.Versioning;
using Infrastructure;
using Persistence;
using Application;
using Serilog;
using Scalar.AspNetCore;
using Carter;
using Microsoft.Extensions.DependencyInjection;

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

if (!string.IsNullOrEmpty(builder.Configuration["ASPNETCORE_HTTPS_PORTS"])
    || !string.IsNullOrEmpty(builder.Configuration["HTTPS_PORT"]))
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
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