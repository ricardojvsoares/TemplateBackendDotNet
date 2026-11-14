using Infrastructure;
using Infrastructure.Telemetry;
using Persistence;
using Serilog;

DotNetEnv.Env.NoClobber().TraversePath().Load();

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog((services, configuration) =>
    configuration
        .ReadFrom.Configuration(builder.Configuration)
        .WriteToOpenTelemetry(builder.Configuration, "Scheduler"));

builder.Services
    .AddAppTelemetry(builder.Configuration, "Scheduler", instrumentAspNetCore: false)
    .AddCache()
    .AddPersistence()
    .AddJobs();

await builder.Build().RunAsync();
