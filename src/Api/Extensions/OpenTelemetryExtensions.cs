using System.Diagnostics;
using System.Diagnostics.Metrics;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace TodoApp.Api.Extensions;

public static class TelemetryConstants
{
    public const string ServiceName = "TodoApp.Api";
    public static readonly ActivitySource ActivitySource = new(ServiceName);
    public static readonly Meter Meter = new(ServiceName);
    
    public static readonly Counter<int> TodoItemsCreated = Meter.CreateCounter<int>("todo.items.created", description: "Number of todo items created");
    public static readonly Counter<int> TodoItemsCompleted = Meter.CreateCounter<int>("todo.items.completed", description: "Number of todo items marked as completed");
    public static readonly Histogram<double> RequestDuration = Meter.CreateHistogram<double>("request.duration", unit: "ms", description: "Duration of requests");
}


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
