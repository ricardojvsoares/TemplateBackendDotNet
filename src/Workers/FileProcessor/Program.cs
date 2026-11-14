using FileProcessor;
using Infrastructure;
using Infrastructure.Telemetry;
using Serilog;

DotNetEnv.Env.NoClobber().TraversePath().Load();
HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog((services, configuration) =>
    configuration
        .ReadFrom.Configuration(builder.Configuration)
        .WriteToOpenTelemetry(builder.Configuration, "FileProcessor"));

builder.Services
    .AddAppTelemetry(builder.Configuration, "FileProcessor", instrumentAspNetCore: false)
    .AddStorage()
    .AddMessaging();

builder.Services.AddHostedService<FileProcessorWorker>();

await builder.Build().RunAsync();