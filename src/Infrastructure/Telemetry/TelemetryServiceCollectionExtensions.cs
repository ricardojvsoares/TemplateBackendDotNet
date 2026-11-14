using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Infrastructure.Telemetry;

public static class TelemetryServiceCollectionExtensions
{
    private const string WolverineActivitySourceName = "Wolverine";

    public static IServiceCollection AddAppTelemetry(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName,
        bool instrumentAspNetCore = false)
    {
        string resolvedServiceName = TelemetryExport.ResolveServiceName(configuration, serviceName);

        IOpenTelemetryBuilder builder = services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(resolvedServiceName))
            .WithTracing(tracing =>
            {
                tracing.AddHttpClientInstrumentation();
                tracing.AddSource(WolverineActivitySourceName);

                if (instrumentAspNetCore)
                {
                    tracing.AddAspNetCoreInstrumentation();
                }
            })
            .WithMetrics(metrics =>
            {
                metrics.AddHttpClientInstrumentation();
                metrics.AddRuntimeInstrumentation();

                if (instrumentAspNetCore)
                {
                    metrics.AddAspNetCoreInstrumentation();
                }
            });

        if (TelemetryExport.IsEnabled(configuration))
        {
            builder.UseOtlpExporter();
        }

        return services;
    }
}
