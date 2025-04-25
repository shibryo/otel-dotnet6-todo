# 第4章：高度な機能と運用 実装アドバイス

## サンプリング設定の実装

### 1. 環境別サンプリング設定

```csharp
public static class SamplingConfigurator
{
    public static TracerProviderBuilder ConfigureSampling(
        this TracerProviderBuilder builder,
        IWebHostEnvironment env)
    {
        var samplingRate = env.IsDevelopment() ? 1.0 : 0.1;
        
        return builder.SetSampler(new CompositeCompositionSampler(
            new ParentBasedSampler(
                new TraceIdRatioBasedSampler(samplingRate)),
            new CustomSampler(samplingRate)));
    }
}

// Program.csでの使用
builder.Services.AddOpenTelemetry()
    .WithTracing(builder => builder
        .ConfigureSampling(app.Environment));
```

### 2. カスタムサンプラーの実装

```csharp
public class CustomSampler : Sampler
{
    private readonly double _defaultSamplingRate;
    private readonly ISet<string> _importantOperations;
    private readonly ISet<string> _errorTags;

    public CustomSampler(double defaultSamplingRate)
    {
        _defaultSamplingRate = defaultSamplingRate;
        _importantOperations = new HashSet<string> 
        { 
            "CreateTodoItem",
            "DeleteTodoItem" 
        };
        _errorTags = new HashSet<string> 
        { 
            "error",
            "exception" 
        };
    }

    public override SamplingResult ShouldSample(
        in SamplingParameters parameters)
    {
        // 重要な操作は常にサンプリング
        if (_importantOperations.Contains(parameters.Name))
        {
            return new SamplingResult(true);
        }

        // エラータグがある場合は常にサンプリング
        if (parameters.Tags.Any(tag => _errorTags.Contains(tag.Key)))
        {
            return new SamplingResult(true);
        }

        // デフォルトのサンプリングレート
        return new SamplingResult(
            Random.Shared.NextDouble() < _defaultSamplingRate);
    }
}
```

## メトリクス収集の実装

### 1. メトリクスの定義

```csharp
public class TodoMetrics
{
    private readonly Meter _meter;
    private readonly Histogram<double> _operationDuration;
    private readonly Counter<long> _errorCount;
    private readonly Counter<long> _operationCount;
    private readonly ObservableGauge<int> _activeTodoItems;

    public TodoMetrics(string meterName = "TodoApi")
    {
        _meter = new Meter(meterName);

        // 操作時間の分布
        _operationDuration = _meter.CreateHistogram<double>(
            "todo.operation.duration",
            unit: "ms",
            description: "Duration of todo operations"
        );

        // エラーカウンター
        _errorCount = _meter.CreateCounter<long>(
            "todo.errors",
            unit: "errors",
            description: "Number of errors occurred"
        );

        // 操作カウンター
        _operationCount = _meter.CreateCounter<long>(
            "todo.operations",
            unit: "operations",
            description: "Number of operations performed"
        );

        // アクティブなTodoアイテム数
        _activeTodoItems = _meter.CreateObservableGauge(
            "todo.items.active",
            () => GetActiveTodoCount());
    }

    private int GetActiveTodoCount()
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TodoContext>();
        return context.TodoItems.Count(t => !t.IsComplete);
    }
}
```

### 2. メトリクス収集の実装

```csharp
public class TodoItemsController : ControllerBase
{
    private readonly TodoMetrics _metrics;
    private readonly ILogger<TodoItemsController> _logger;
    private readonly Stopwatch _stopwatch;

    public TodoItemsController(
        TodoMetrics metrics,
        ILogger<TodoItemsController> logger)
    {
        _metrics = metrics;
        _logger = logger;
        _stopwatch = new Stopwatch();
    }

    [HttpPost]
    public async Task<ActionResult<TodoItem>> CreateTodoItem(
        TodoItem todoItem)
    {
        _stopwatch.Restart();
        
        try
        {
            var result = await CreateTodoItemInternal(todoItem);
            
            _metrics.RecordOperation(
                "create",
                _stopwatch.ElapsedMilliseconds);
                
            return result;
        }
        catch (Exception ex)
        {
            _metrics.RecordOperation(
                "create",
                _stopwatch.ElapsedMilliseconds,
                isError: true);
                
            _logger.LogError(ex, "Failed to create todo item");
            throw;
        }
        finally
        {
            _stopwatch.Stop();
        }
    }
}
```

## エラーハンドリングの実装

### 1. グローバルエラーハンドラー

```csharp
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly TodoMetrics _metrics;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger,
        TodoMetrics metrics)
    {
        _logger = logger;
        _metrics = metrics;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var errorId = Guid.NewGuid();
        var activity = Activity.Current;

        // エラー情報の記録
        _logger.LogError(
            exception,
            "Error {ErrorId} occurred. TraceId: {TraceId}",
            errorId,
            activity?.TraceId);

        // メトリクスの記録
        _metrics.RecordError(
            exception.GetType().Name,
            activity?.Duration.TotalMilliseconds ?? 0);

        // エラーレスポンスの作成
        var problem = new ProblemDetails
        {
            Status = GetStatusCode(exception),
            Title = "An error occurred",
            Detail = GetSafeErrorMessage(exception),
            Instance = context.Request.Path,
            Extensions = 
            {
                ["errorId"] = errorId,
                ["traceId"] = activity?.TraceId.ToString()
            }
        };

        await Results.Problem(problem)
            .ExecuteAsync(context);

        return true;
    }

    private static int GetStatusCode(Exception exception) =>
        exception switch
        {
            ValidationException => StatusCodes.Status400BadRequest,
            NotFoundException => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status500InternalServerError
        };

    private static string GetSafeErrorMessage(Exception exception) =>
        exception switch
        {
            ValidationException => "Invalid request data",
            NotFoundException => "Requested resource not found",
            _ => "An unexpected error occurred"
        };
}
```

## パフォーマンス最適化

### 1. データベース最適化

```csharp
public class TodoRepository
{
    private readonly TodoContext _context;
    private readonly IMemoryCache _cache;
    private readonly TodoMetrics _metrics;

    public TodoRepository(
        TodoContext context,
        IMemoryCache cache,
        TodoMetrics metrics)
    {
        _context = context;
        _cache = cache;
        _metrics = metrics;
    }

    public async Task<TodoItem?> GetByIdAsync(long id)
    {
        var cacheKey = $"todo:{id}";
        
        if (_cache.TryGetValue<TodoItem>(cacheKey, out var item))
        {
            _metrics.RecordCacheHit();
            return item;
        }

        _metrics.RecordCacheMiss();
        
        item = await _context.TodoItems
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);

        if (item != null)
        {
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(5));
                
            _cache.Set(cacheKey, item, cacheOptions);
        }

        return item;
    }

    public async Task<List<TodoItem>> GetAllAsync()
    {
        return await _context.TodoItems
            .AsNoTracking()
            .TagWith("Get all todo items")
            .ToListAsync();
    }

    public async Task<List<TodoItem>> GetBatchAsync(
        IEnumerable<long> ids)
    {
        return await _context.TodoItems
            .AsNoTracking()
            .Where(t => ids.Contains(t.Id))
            .TagWith("Get batch todo items")
            .ToListAsync();
    }
}
```

### 2. バッチ処理の最適化

```csharp
public class BatchOperationService
{
    private readonly TodoContext _context;
    private readonly TodoMetrics _metrics;
    private readonly ILogger<BatchOperationService> _logger;

    public BatchOperationService(
        TodoContext context,
        TodoMetrics metrics,
        ILogger<BatchOperationService> logger)
    {
        _context = context;
        _metrics = metrics;
        _logger = logger;
    }

    public async Task<BatchOperationResult> CompleteManyAsync(
        IEnumerable<long> ids)
    {
        using var activity = Telemetry.ActivitySource
            .StartActivity("CompleteManyTodoItems");
            
        activity?.SetTag("todo.items.count", ids.Count());

        var sw = Stopwatch.StartNew();
        var result = new BatchOperationResult();

        try
        {
            // バッチサイズの最適化
            const int batchSize = 100;
            var idBatches = ids
                .Chunk(batchSize)
                .ToList();

            foreach (var batch in idBatches)
            {
                var items = await _context.TodoItems
                    .Where(t => batch.Contains(t.Id))
                    .ToListAsync();

                foreach (var item in items)
                {
                    item.IsComplete = true;
                    result.ProcessedCount++;
                }

                await _context.SaveChangesAsync();
            }

            _metrics.RecordBatchOperation(
                "complete_many",
                result.ProcessedCount,
                sw.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            _metrics.RecordBatchOperation(
                "complete_many_failed",
                result.ProcessedCount,
                sw.ElapsedMilliseconds);

            _logger.LogError(
                ex,
                "Failed to complete todos. ProcessedCount: {Count}",
                result.ProcessedCount);

            throw;
        }
    }
}
```

## デバッグとトラブルシューティング

### 1. 診断ツールの実装

```csharp
public class DiagnosticCollector
{
    private readonly ILogger<DiagnosticCollector> _logger;
    private readonly TodoMetrics _metrics;
    private readonly ConcurrentDictionary<string, List<double>> _timings;

    public DiagnosticCollector(
        ILogger<DiagnosticCollector> logger,
        TodoMetrics metrics)
    {
        _logger = logger;
        _metrics = metrics;
        _timings = new ConcurrentDictionary<string, List<double>>();
    }

    public void RecordTiming(string operation, double milliseconds)
    {
        _timings.AddOrUpdate(
            operation,
            _ => new List<double> { milliseconds },
            (_, list) =>
            {
                list.Add(milliseconds);
                return list;
            });
    }

    public void AnalyzePerformance()
    {
        foreach (var (operation, timings) in _timings)
        {
            var avg = timings.Average();
            var p95 = CalculatePercentile(timings, 95);
            var p99 = CalculatePercentile(timings, 99);

            _logger.LogInformation(
                "Operation: {Operation}, Avg: {Avg}ms, P95: {P95}ms, P99: {P99}ms",
                operation, avg, p95, p99);
        }
    }

    private static double CalculatePercentile(
        List<double> timings,
        double percentile)
    {
        var sortedTimings = timings.OrderBy(t => t).ToList();
        var index = (int)Math.Ceiling(percentile / 100.0 * sortedTimings.Count) - 1;
        return sortedTimings[index];
    }
}
```

### 2. パフォーマンス監視の実装

```csharp
public class PerformanceMonitor
{
    private readonly TodoMetrics _metrics;
    private readonly ILogger<PerformanceMonitor> _logger;
    private readonly DiagnosticCollector _diagnostics;
    private readonly PerformanceOptions _options;

    public PerformanceMonitor(
        TodoMetrics metrics,
        ILogger<PerformanceMonitor> logger,
        DiagnosticCollector diagnostics,
        IOptions<PerformanceOptions> options)
    {
        _metrics = metrics;
        _logger = logger;
        _diagnostics = diagnostics;
        _options = options.Value;
    }

    public async Task MonitorOperationAsync(
        Func<Task> operation,
        string operationName)
    {
        var sw = Stopwatch.StartNew();
        
        try
        {
            await operation();
        }
        finally
        {
            sw.Stop();
            var duration = sw.ElapsedMilliseconds;

            _diagnostics.RecordTiming(operationName, duration);

            if (duration > _options.SlowOperationThreshold)
            {
                _logger.LogWarning(
                    "Slow operation detected: {Operation} took {Duration}ms",
                    operationName,
                    duration);
                    
                _metrics.RecordSlowOperation(operationName, duration);
            }
        }
    }
}
```

## セキュリティ考慮事項

### 1. 機密情報の取り扱い

```csharp
public static class TelemetryExtensions
{
    public static void SetSafeTag(
        this Activity activity,
        string key,
        string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        // 機密情報のマスク処理
        var maskedValue = key.ToLower() switch
        {
            var k when k.Contains("password") => "***",
            var k when k.Contains("token") => "***",
            var k when k.Contains("key") => "***",
            _ when value.Length > 100 => value[..97] + "...",
            _ => value
        };

        activity?.SetTag(key, maskedValue);
    }
}
```

### 2. エラー情報の制御

```csharp
public static class ErrorHandler
{
    public static string GetSafeErrorMessage(
        Exception ex,
        IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            return ex.ToString();
        }

        return ex switch
        {
            ValidationException => "Invalid request data",
            DbUpdateException => "Database operation failed",
            _ => "An unexpected error occurred"
        };
    }

    public static void LogError(
        ILogger logger,
        Exception ex,
        string operationName)
    {
        var errorContext = new
        {
            Operation = operationName,
            ErrorType = ex.GetType().Name,
            Message = ex.Message,
            TraceId = Activity.Current?.TraceId.ToString()
        };

        logger.LogError(
            ex,
            "Error in {Operation}. Type: {ErrorType}. TraceId: {TraceId}",
            errorContext.Operation,
            errorContext.ErrorType,
            errorContext.TraceId);
    }
}
