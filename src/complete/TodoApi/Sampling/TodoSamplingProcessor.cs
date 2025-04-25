using OpenTelemetry;
using OpenTelemetry.Trace;
using System.Diagnostics;

namespace TodoApi.Sampling;

public class TodoSamplingProcessor : BaseProcessor<Activity>
{
    private readonly double _defaultSamplingRatio;
    private readonly HashSet<string> _importantEndpoints;

    public TodoSamplingProcessor(double defaultSamplingRatio = 0.1)
    {
        _defaultSamplingRatio = defaultSamplingRatio;
        _importantEndpoints = new HashSet<string>
        {
            "/api/TodoItems/Create",
            "/api/TodoItems/Delete"
        };
    }

    public override void OnStart(Activity activity)
    {
        if (activity == null) return;

        // Always sample if it's an important endpoint
        if (IsImportantEndpoint(activity))
        {
            activity.ActivityTraceFlags |= ActivityTraceFlags.Recorded;
            return;
        }

        // Always sample if there's an error
        if (HasError(activity))
        {
            activity.ActivityTraceFlags |= ActivityTraceFlags.Recorded;
            return;
        }

        // Apply default sampling ratio for other cases
        if (Random.Shared.NextDouble() < _defaultSamplingRatio)
        {
            activity.ActivityTraceFlags |= ActivityTraceFlags.Recorded;
        }
        else
        {
            activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
        }
    }

    private bool IsImportantEndpoint(Activity activity)
    {
        var httpRoute = activity.GetTagItem("http.route") as string;
        return !string.IsNullOrEmpty(httpRoute) && _importantEndpoints.Contains(httpRoute);
    }

    private bool HasError(Activity activity)
    {
        var statusCode = activity.GetTagItem("http.status_code") as string;
        return !string.IsNullOrEmpty(statusCode) && 
               (statusCode.StartsWith("4") || statusCode.StartsWith("5"));
    }

    public override void OnEnd(Activity activity)
    {
        // No additional processing needed on end
    }
}
