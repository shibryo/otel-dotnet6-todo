using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace TodoApp.Api.Extensions;

public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddOpenTelemetryServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService("TodoApp.Api")
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
                .AddPrometheusExporter());

        return services;
    }
}
