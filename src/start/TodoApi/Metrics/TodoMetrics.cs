using System.Diagnostics.Metrics;

namespace TodoApi.Metrics;

public class TodoMetrics
{
    private readonly Meter _meter;
    private readonly Counter<int> _todosCreatedCounter;
    private readonly Counter<int> _todosCompletedCounter;

    public TodoMetrics()
    {
        _meter = new Meter("TodoApi");
        _todosCreatedCounter = _meter.CreateCounter<int>(
            "todos.created_total",
            description: "Total number of todos created");
            
        _todosCompletedCounter = _meter.CreateCounter<int>(
            "todos.completed_total",
            description: "Total number of todos completed");
    }

    public void TodoCreated() => _todosCreatedCounter.Add(1);
    public void TodoCompleted() => _todosCompletedCounter.Add(1);
}
