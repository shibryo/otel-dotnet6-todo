using System.Diagnostics;
using MediatR;
using TodoApp.Api.Extensions;

namespace TodoApp.Application.Common.Behaviors;

public class TracingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ActivitySource _activitySource;

    public TracingBehavior(ActivitySource activitySource)
    {
        _activitySource = activitySource;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        using var activity = _activitySource.StartActivity($"Handle{requestName}");
        
        try
        {
            var startTime = DateTime.UtcNow;
            var response = await next();
            TelemetryConstants.RequestDuration.Record((DateTime.UtcNow - startTime).TotalMilliseconds,
                new KeyValuePair<string, object?>("request_type", requestName));
            
            activity?.SetTag("request.success", true);
            return response;
        }
        catch (Exception ex)
        {
            activity?.SetTag("request.success", false);
            activity?.SetTag("error.message", ex.Message);
            activity?.SetTag("error.type", ex.GetType().Name);
            throw;
        }
    }
}
