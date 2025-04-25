using System.Diagnostics.Metrics;
using OpenTelemetry;
using OpenTelemetry.Metrics;

namespace TodoApi.Metrics;

public class TodoMetrics
{
    private readonly Counter<int> _todosCreatedCounter;
    private readonly Counter<int> _todosCompletedCounter;
    private readonly UpDownCounter<int> _activeTodosCounter;
    private readonly Histogram<double> _todoCompletionTimeHistogram;
    private readonly Counter<int> _todoOperationErrorCounter;
    private readonly Histogram<double> _apiResponseTimeHistogram;
    private readonly Counter<int> _todoOperationCounter;

    private readonly Meter _meter;

    public TodoMetrics()
    {
        _meter = new Meter("TodoApi");
        
        _todosCreatedCounter = _meter.CreateCounter<int>(
            "todo.created",
            description: "Number of todo items created");

        _todosCompletedCounter = _meter.CreateCounter<int>(
            "todo.completed",
            description: "Number of todo items marked as complete");

        _activeTodosCounter = _meter.CreateUpDownCounter<int>(
            "todo.active",
            description: "Number of active (incomplete) todo items");

        _todoCompletionTimeHistogram = _meter.CreateHistogram<double>(
            "todo.completion_time",
            unit: "ms",
            description: "Time taken to complete todo items");

        _todoOperationErrorCounter = _meter.CreateCounter<int>(
            "todo.operation.errors",
            description: "Number of failed todo operations");

        _apiResponseTimeHistogram = _meter.CreateHistogram<double>(
            "todo.api.response_time",
            unit: "ms",
            description: "API response time for todo operations");

        _todoOperationCounter = _meter.CreateCounter<int>(
            "todo.operation.count",
            description: "Number of todo operations performed");
    }

    public void TodoCreated(string priority = "normal")
    {
        _todosCreatedCounter.Add(1);
        _activeTodosCounter.Add(1);
        _todoOperationCounter.Add(1, new KeyValuePair<string, object>[] 
        {
            new("operation", "create"),
            new("priority", priority)
        });
    }

    public void TodoCompleted(DateTime createdAt, string priority = "normal")
    {
        _todosCompletedCounter.Add(1);
        _activeTodosCounter.Add(-1);
        
        var completionTime = (DateTime.UtcNow - createdAt).TotalMilliseconds;
        _todoCompletionTimeHistogram.Record(completionTime);
        _todoOperationCounter.Add(1, new KeyValuePair<string, object>[] 
        {
            new("operation", "complete"),
            new("priority", priority)
        });
    }

    public void TodoUncompleted(string priority = "normal")
    {
        _activeTodosCounter.Add(1);
        _todoOperationCounter.Add(1, new KeyValuePair<string, object>[] 
        {
            new("operation", "uncomplete"),
            new("priority", priority)
        });
    }

    public void TodoDeleted(bool wasCompleted, string priority = "normal")
    {
        if (!wasCompleted)
        {
            _activeTodosCounter.Add(-1);
        }
        _todoOperationCounter.Add(1, new KeyValuePair<string, object>[] 
        {
            new("operation", "delete"),
            new("priority", priority)
        });
    }

    public void RecordOperationError(string operation, string errorType)
    {
        _todoOperationErrorCounter.Add(1, new KeyValuePair<string, object>[] 
        {
            new("operation", operation),
            new("error_type", errorType)
        });
    }

    public void RecordApiResponseTime(double milliseconds, string operation)
    {
        _apiResponseTimeHistogram.Record(milliseconds, new KeyValuePair<string, object>[] 
        {
            new("operation", operation)
        });
    }
}
