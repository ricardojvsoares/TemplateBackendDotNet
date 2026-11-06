using FileProcessor;
using Infrastructure;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddStorage()
    .AddMessaging();

builder.Services.AddHostedService<FileProcessorWorker>();

await builder.Build().RunAsync();
