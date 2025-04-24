using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using TodoApp.Infrastructure.Telemetry;

namespace TodoApp.Api.Extensions;

public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddOpenTelemetryServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(TelemetryConstants.ServiceName)
            .AddTelemetrySdk()
            .AddEnvironmentVariableDetector();

        services.AddOpenTelemetry()
            .WithTracing(builder => builder
                .SetResourceBuilder(resourceBuilder)
                .AddAspNetCoreInstrumentation()
                .AddEntityFrameworkCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddJaegerExporter(options =>
                {
                    options.AgentHost = configuration["OpenTelemetry:Jaeger:AgentHost"] ?? "localhost";
                    options.AgentPort = int.Parse(configuration["OpenTelemetry:Jaeger:AgentPort"] ?? "6831");
                }))
            .WithMetrics(builder => builder
                .SetResourceBuilder(resourceBuilder)
                .AddAspNetCoreInstrumentation()
                .AddRuntimeInstrumentation()
                .AddHttpClientInstrumentation()
                .AddMeter(TelemetryConstants.Meter.Name)
                .AddPrometheusExporter());

        // Register ActivitySource
        services.AddSingleton(TelemetryConstants.ActivitySource);

        return services;
    }
}
