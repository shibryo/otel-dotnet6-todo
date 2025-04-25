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
    }

    public void TodoCreated()
    {
        _todosCreatedCounter.Add(1);
        _activeTodosCounter.Add(1);
    }

    public void TodoCompleted(DateTime createdAt)
    {
        _todosCompletedCounter.Add(1);
        _activeTodosCounter.Add(-1);
        
        var completionTime = (DateTime.UtcNow - createdAt).TotalMilliseconds;
        _todoCompletionTimeHistogram.Record(completionTime);
    }

    public void TodoUncompleted()
    {
        _activeTodosCounter.Add(1);
    }

    public void TodoDeleted(bool wasCompleted)
    {
        if (!wasCompleted)
        {
            _activeTodosCounter.Add(-1);
        }
    }
}
