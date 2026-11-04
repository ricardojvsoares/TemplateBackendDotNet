using Infrastructure;
using Persistence;
using Application;
using Serilog;
using Scalar.AspNetCore;
using Carter;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();


builder.Host.UseSerilog(
    (context, configuration) =>
        configuration.ReadFrom.Configuration(
            context.Configuration));

builder.Services
    .AddInfrastructure()
    .AddPersistence()
    .AddApplication();

builder.Services.AddCarter();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.MapScalarApiReference("/docs", options =>
    {
        options.WithTitle("Api");
        options.WithOpenApiRoutePattern("/openapi/{documentName}.json");
    });
}

app.UseHttpsRedirection();

app.MapCarter();

await app.RunAsync();
