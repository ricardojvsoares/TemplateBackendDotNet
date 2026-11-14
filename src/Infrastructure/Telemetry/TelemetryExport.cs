using Microsoft.Extensions.Configuration;

namespace Infrastructure.Telemetry;

internal static class TelemetryExport
{
    internal const string EndpointVariable = "OTEL_EXPORTER_OTLP_ENDPOINT";

    internal const string ProtocolVariable = "OTEL_EXPORTER_OTLP_PROTOCOL";

    internal const string ServiceNameVariable = "OTEL_SERVICE_NAME";

    internal const string SdkDisabledVariable = "OTEL_SDK_DISABLED";

    internal static bool IsEnabled(IConfiguration configuration)
    {
        if (IsSdkDisabled(configuration))
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(configuration[EndpointVariable]);
    }

    internal static string ResolveServiceName(IConfiguration configuration, string serviceName)
    {
        string? configured = configuration[ServiceNameVariable];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        return serviceName;
    }

    private static bool IsSdkDisabled(IConfiguration configuration)
    {
        return string.Equals(
            configuration[SdkDisabledVariable],
            "true",
            StringComparison.OrdinalIgnoreCase);
    }
}
