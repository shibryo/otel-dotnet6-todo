using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace TodoApp.Infrastructure.Telemetry;

public static class TelemetryConstants
{
    public const string ServiceName = "TodoApp.Api";
    public static readonly ActivitySource ActivitySource = new(ServiceName);
    public static readonly Meter Meter = new(ServiceName);
    
    public static readonly Counter<int> TodoItemsCreated = Meter.CreateCounter<int>("todo.items.created", description: "Number of todo items created");
    public static readonly Counter<int> TodoItemsCompleted = Meter.CreateCounter<int>("todo.items.completed", description: "Number of todo items marked as completed");
    public static readonly Histogram<double> RequestDuration = Meter.CreateHistogram<double>("request.duration", unit: "ms", description: "Duration of requests");
}
