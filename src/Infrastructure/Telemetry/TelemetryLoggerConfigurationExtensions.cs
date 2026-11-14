using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Sinks.OpenTelemetry;

namespace Infrastructure.Telemetry;

public static class TelemetryLoggerConfigurationExtensions
{
    private const string TraceIdPropertyName = "TraceId";

    private const string HttpProtobufProtocol = "http/protobuf";

    public static LoggerConfiguration WriteToOpenTelemetry(
        this LoggerConfiguration loggerConfiguration,
        IConfiguration configuration,
        string serviceName)
    {
        loggerConfiguration = loggerConfiguration
            .Enrich.WithSpan()
            .Enrich.WithProperty(TraceIdPropertyName, string.Empty);

        if (!TelemetryExport.IsEnabled(configuration))
        {
            return loggerConfiguration;
        }

        string endpoint = configuration[TelemetryExport.EndpointVariable] ?? string.Empty;
        string resolvedServiceName = TelemetryExport.ResolveServiceName(configuration, serviceName);

        return loggerConfiguration.WriteTo.OpenTelemetry(options =>
        {
            options.Endpoint = endpoint;
            options.Protocol = ReadProtocol(configuration);
            options.ResourceAttributes = new Dictionary<string, object>
            {
                ["service.name"] = resolvedServiceName
            };
        });
    }

    private static OtlpProtocol ReadProtocol(IConfiguration configuration)
    {
        if (string.Equals(
            configuration[TelemetryExport.ProtocolVariable],
            HttpProtobufProtocol,
            StringComparison.OrdinalIgnoreCase))
        {
            return OtlpProtocol.HttpProtobuf;
        }

        return OtlpProtocol.Grpc;
    }
}
