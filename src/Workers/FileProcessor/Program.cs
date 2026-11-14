using FileProcessor;
using Infrastructure;
using Serilog;

DotNetEnv.Env.NoClobber().TraversePath().Load();
HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog((services, configuration) =>
    configuration.ReadFrom.Configuration(builder.Configuration));

builder.Services
    .AddStorage()
    .AddMessaging();

builder.Services.AddHostedService<FileProcessorWorker>();

await builder.Build().RunAsync();