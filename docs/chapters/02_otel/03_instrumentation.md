# 自動計装とカスタム計装

## 概要

この章では、OpenTelemetryの自動計装機能を活用しながら、Todoアプリケーション固有の情報を収集するためのカスタム計装を実装します。

## 自動計装

### ASP.NET Coreの自動計装

ASP.NET Coreの自動計装により、HTTPリクエストに関する情報が自動的に収集されます。

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(builder => builder
        .AddAspNetCoreInstrumentation(options =>
        {
            // リクエストフィルタリング
            options.Filter = (ctx) =>
            {
                // ヘルスチェックは除外
                return !ctx.Request.Path.StartsWithSegments("/health");
            };

            // エンリッチメント（情報の追加）
            options.EnrichWithHttpRequest = (activity, request) =>
            {
                activity.SetTag("todo.tenant", request.Headers["X-Tenant"]);
            };
        }));
```

収集される情報：
- HTTPメソッド
- URLパス
- レスポンスステータスコード
- 処理時間
- ユーザーエージェント

### Entity Framework Coreの自動計装

データベース操作の追跡を自動化します。

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(builder => builder
        .AddEntityFrameworkCoreInstrumentation(options =>
        {
            // コマンドテキストの記録
            options.SetDbStatementForText = true;
            
            // フィルタリング
            options.Filter = (command) =>
            {
                // 特定のテーブルへのアクセスのみを記録
                return command.CommandText.Contains("TodoItems");
            };
        }));
```

収集される情報：
- SQLクエリ
- データベース名
- 処理時間
- エラー情報

### HTTPクライアントの自動計装

外部APIとの通信を追跡します。

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(builder => builder
        .AddHttpClientInstrumentation(options =>
        {
            // フィルタリング
            options.FilterHttpRequestMessage = (request) =>
            {
                // 特定のエンドポイントへのリクエストのみを記録
                return request.RequestUri?.Host == "api.example.com";
            };

            // エンリッチメント
            options.EnrichWithHttpRequestMessage = (activity, request) =>
            {
                activity.SetTag("http.request_id", request.Headers.GetValues("X-Request-ID").FirstOrDefault());
            };
        }));
```

## カスタム計装

### ActivitySourceの設定

```csharp
public static class Telemetry
{
    public static readonly ActivitySource Source = new("TodoApi");
}
```

### コントローラーでのカスタム計装

```csharp
[ApiController]
[Route("api/[controller]")]
public class TodoItemsController : ControllerBase
{
    private readonly TodoContext _context;
    private readonly TodoMetrics _metrics;

    public TodoItemsController(TodoContext context, TodoMetrics metrics)
    {
        _context = context;
        _metrics = metrics;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TodoItem>>> GetTodoItems()
    {
        using var activity = Telemetry.Source.StartActivity("GetTodoItems");
        
        try
        {
            var items = await _context.TodoItems.ToListAsync();
            activity?.SetTag("todo.count", items.Count);
            return items;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    [HttpPost]
    public async Task<ActionResult<TodoItem>> PostTodoItem(TodoItem todoItem)
    {
        using var activity = Telemetry.Source.StartActivity("CreateTodoItem");
        activity?.SetTag("todo.title", todoItem.Title);

        try
        {
            todoItem.CreatedAt = DateTime.UtcNow;
            _context.TodoItems.Add(todoItem);
            await _context.SaveChangesAsync();
            
            _metrics.TodoCreated();  // メトリクスの記録
            
            activity?.SetTag("todo.id", todoItem.Id);
            return CreatedAtAction(nameof(GetTodoItem), new { id = todoItem.Id }, todoItem);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }
}
```

### カスタムメトリクスの実装

```csharp
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
}
```

## エラーハンドリングの計装

### グローバルエラーハンドラー

```csharp
public class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var activity = Activity.Current;
        
        // エラー情報の記録
        activity?.SetStatus(ActivityStatusCode.Error);
        activity?.SetTag("error.type", exception.GetType().Name);
        activity?.SetTag("error.message", exception.Message);
        
        if (exception is DbUpdateException dbEx)
        {
            activity?.SetTag("error.db_message", dbEx.InnerException?.Message);
        }

        // レスポンスの設定
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new
        {
            Error = "An error occurred",
            TraceId = activity?.TraceId.ToString()
        });

        return true;
    }
}
```

## パフォーマンス分析のための計装

### 処理時間の計測

```csharp
[HttpGet("{id}")]
public async Task<ActionResult<TodoItem>> GetTodoItem(int id)
{
    using var activity = Telemetry.Source.StartActivity(
        "GetTodoItem",
        ActivityKind.Internal,
        parentContext: Activity.Current?.Context ?? default);

    try
    {
        var sw = Stopwatch.StartNew();
        var todoItem = await _context.TodoItems.FindAsync(id);
        sw.Stop();

        if (todoItem == null)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Item not found");
            return NotFound();
        }

        activity?.SetTag("todo.id", id);
        activity?.SetTag("todo.title", todoItem.Title);
        activity?.SetTag("todo.is_complete", todoItem.IsComplete);
        activity?.SetTag("todo.lookup_time_ms", sw.ElapsedMilliseconds);

        return todoItem;
    }
    catch (Exception ex)
    {
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        throw;
    }
}
```

## まとめ

1. 自動計装
   - フレームワークレベルの情報を自動収集
   - フィルタリングとエンリッチメントで必要な情報を制御
   - 基本的なパフォーマンスメトリクスを提供

2. カスタム計装
   - ビジネスロジック固有の情報を収集
   - エラー情報の詳細な記録
   - パフォーマンス分析用の指標追加

## 次のステップ

次章では、メトリクスとログの実装について詳しく説明します。特に、カスタムメトリクスの設計とログの構造化について、実践的な例を交えて解説します。
