using Infrastructure;
using Persistence;
using Serilog;

DotNetEnv.Env.NoClobber().TraversePath().Load();

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog((services, configuration) =>
    configuration.ReadFrom.Configuration(builder.Configuration));

builder.Services
    .AddCache()
    .AddPersistence()
    .AddJobs();

await builder.Build().RunAsync();
