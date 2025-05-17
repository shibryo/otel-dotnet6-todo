# 第2章：OpenTelemetryの導入 - 実装アドバイス

## 章の学習目標

1. OpenTelemetryの基本概念の理解
   - 分散トレーシングの仕組み
   - メトリクス収集の仕組み
   - ログ連携の重要性

2. .NET環境での実装スキルの習得
   - ActivitySourceの使用方法
   - メトリクスAPIの活用
   - ログとトレースの連携方法

3. 実装上の重要ポイント
   - 適切なSpan設計
   - エラーハンドリング
   - パフォーマンスへの考慮

## セッション別コンテンツ

### 1. SDK導入
参考資料：
- [OTel .NET SDK概要](https://opentelemetry.io/docs/instrumentation/net/)
- [自動計装ガイド](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry.Instrumentation.AspNetCore/README.md)

実装のポイント：
1. パッケージ導入（15分）
   ```bash
   dotnet add package OpenTelemetry
   dotnet add package OpenTelemetry.Extensions.Hosting
   dotnet add package OpenTelemetry.Instrumentation.AspNetCore
   dotnet add package OpenTelemetry.Instrumentation.Http
   dotnet add package OpenTelemetry.Instrumentation.EntityFrameworkCore
   ```
   - 各パッケージの役割を説明
   - バージョンの選択理由を解説
   - 参考：[OpenTelemetry .NET入門](https://opentelemetry.io/docs/instrumentation/net/getting-started/)

2. 基本設定の実装（30分）
   ```csharp
   builder.Services.AddOpenTelemetry()
       .WithTracing(tracerProvider => tracerProvider
           .SetResourceBuilder(ResourceBuilder.CreateDefault()
               .AddService("TodoApi"))
           .AddAspNetCoreInstrumentation()
           .AddHttpClientInstrumentation()
           .AddEntityFrameworkCoreInstrumentation());
   ```
   - 設定の各部分の意味を説明
   - リソース属性の重要性
   - 参考：[Trace API](https://opentelemetry.io/docs/reference/specification/trace/api/)

3. 動作確認（15分）
   - サンプルリクエストの送信
   - 自動計装の確認方法
   - 参考：[トラブルシューティングガイド](https://opentelemetry.io/docs/instrumentation/net/troubleshooting/)

### セッション2: トレース実装（60分）

#### 概要と目的
- カスタムトレースの実装方法
- Activity APIの使用方法
- エラー処理の計装

#### 技術的背景
Activity APIは.NETでのOpenTelemetryトレース実装の中核です：
- ActivitySourceがトレーサーの役割
- Activityがスパンに相当
- 処理のコンテキスト伝搬を自動管理

参考：[Activity APIドキュメント](https://learn.microsoft.com/ja-jp/dotnet/core/diagnostics/distributed-tracing-concepts)

#### 実装手順と説明
1. ActivitySourceの設定（15分）
   ```csharp
   public static class Telemetry
   {
       public static readonly ActivitySource ActivitySource = 
           new("TodoApi.Activities");
   }
   ```
   - ActivitySourceの役割の説明
   - 名前空間の設計方針
   - 参考：[ActivitySource仕様](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/trace/api.md#tracer)

2. CRUD操作の計装実装（30分）
   - 各APIエンドポイントでのトレース実装
   - タグ付けとコンテキスト情報の追加
   - エラーハンドリングの統合

3. 動作確認とデバッグ（15分）
   - トレースの可視化
   - エラー時の動作確認
   - トラブルシューティング方法

### セッション3: メトリクス実装（60分）

#### 概要と目的
- メトリクス収集の基本概念
- カウンターとヒストグラムの実装
- パフォーマンス測定の統合

#### 技術的背景
.NETのメトリクスAPIについて：
- System.Diagnostics.Metricsの基本概念
- メトリクスの種類と使い分け
- データ集計の仕組み

参考：[Metrics API仕様](https://opentelemetry.io/docs/reference/specification/metrics/api/)

#### 実装手順と説明
1. メトリクスの設計（15分）
   - 収集するメトリクスの選定
   - 命名規則の説明
   - タグ（ラベル）の設計

2. 実装演習（30分）
   ```csharp
   public class TodoMetrics
   {
       private readonly Counter<long> _todoItemsCreated;
       private readonly Histogram<double> _operationDuration;

       public TodoMetrics(Meter meter)
       {
           _todoItemsCreated = meter.CreateCounter<long>(
               "todo.items.created",
               description: "Number of todo items created");
           
           _operationDuration = meter.CreateHistogram<double>(
               "todo.operation.duration",
               unit: "ms",
               description: "Duration of todo operations");
       }
   }
   ```
   - カウンターの実装と使用方法
   - ヒストグラムの実装と計測方法
   - パフォーマンス影響の考慮

3. 確認とチューニング（15分）
   - メトリクス収集の確認
   - 集計間隔の調整
   - 負荷テストでの検証

### セッション4: ログ連携（60分）

#### 概要と目的
- 構造化ログの実装
- トレースIDとの連携
- ログレベルの使い分け

#### 技術的背景
ログとトレースの連携の重要性：
- コンテキスト情報の共有
- トラブルシューティングの効率化
- パフォーマンス分析の容易化

参考：[ログ仕様](https://opentelemetry.io/docs/reference/specification/logs/)

#### 実装手順と説明
1. ログの設定（15分）
   ```csharp
   public static class LogEvents
   {
       public static readonly EventId Created = new(1000, "TodoItemCreated");
       public static readonly EventId Updated = new(1001, "TodoItemUpdated");
       public static readonly EventId Error = new(2000, "TodoOperationError");
   }
   ```
   - イベントIDの設計
   - ログレベルの選定
   - 構造化ログの形式

2. トレース連携実装（30分）
   ```csharp
   _logger.LogInformation(
       "Creating todo item. TraceId: {TraceId}",
       activity?.Context.TraceId);
   ```
   - トレースIDの関連付け
   - コンテキスト情報の追加
   - エラー情報の記録

3. 動作確認（15分）
   - ログの出力確認
   - トレース情報との紐付け
   - 実運用での活用方法

### 1. パッケージ選択の理由
各パッケージの役割と必要性を説明します：

- OpenTelemetry：
  - 基本的な計装機能を提供
  - 参照：[OpenTelemetry概要](https://opentelemetry.io/docs/concepts/what-is-opentelemetry/)

- OpenTelemetry.Extensions.Hosting：
  - .NET依存性注入との統合
  - アプリケーションのライフサイクル管理
  - 参照：[Hosting拡張機能の説明](https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/src/OpenTelemetry.Extensions.Hosting)

- OpenTelemetry.Instrumentation.AspNetCore：
  - WebAPI、HTTPリクエストの自動計装
  - ミドルウェアパイプラインの計装
  - 参照：[ASP.NET Core計装](https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/src/OpenTelemetry.Instrumentation.AspNetCore)

- OpenTelemetry.Instrumentation.EntityFrameworkCore：
  - データベース操作の計装
  - クエリ実行時間の測定
  - 参照：[EF Core計装](https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/src/OpenTelemetry.Instrumentation.EntityFrameworkCore)

### 2. セットアップ手順と説明

#### 2.1 パッケージのインストール

```bash
dotnet add package OpenTelemetry
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package OpenTelemetry.Instrumentation.AspNetCore
dotnet add package OpenTelemetry.Instrumentation.Http
dotnet add package OpenTelemetry.Instrumentation.EntityFrameworkCore
```

### 2. Program.csでの基本設定

```csharp
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// OpenTelemetryの設定
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProvider => tracerProvider
        // リソース情報の設定
        .SetResourceBuilder(ResourceBuilder.CreateDefault()
            .AddService("TodoApi")
            .AddTelemetrySdk()
            .AddEnvironmentVariableDetector())
        // 自動計装の追加
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        // カスタムActivitySourceの追加
        .AddSource("TodoApi.Activities")
        // サンプラーの設定
        .SetSampler(new AlwaysOnSampler()));
```

### 3. ActivitySourceの設定

```csharp
// ActivitySourceの定義
public static class Telemetry
{
    public static readonly ActivitySource ActivitySource = new("TodoApi.Activities");
}

// コントローラーでの使用
public class TodoItemsController : ControllerBase
{
    private readonly ActivitySource _activitySource;

    public TodoItemsController()
    {
        _activitySource = Telemetry.ActivitySource;
    }
}
```

## カスタム計装の実装

### 1. CRUDオペレーションの計装

```csharp
[ApiController]
[Route("api/[controller]")]
public class TodoItemsController : ControllerBase
{
    private readonly TodoContext _context;
    private readonly ActivitySource _activitySource;

    public TodoItemsController(TodoContext context)
    {
        _context = context;
        _activitySource = Telemetry.ActivitySource;
    }

    // GET: api/TodoItems
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TodoItem>>> GetTodoItems()
    {
        using var activity = _activitySource.StartActivity("GetTodoItems");
        try
        {
            var items = await _context.TodoItems.ToListAsync();
            activity?.SetTag("todo.count", items.Count);
            return items;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            throw;
        }
    }

    // POST: api/TodoItems
    [HttpPost]
    public async Task<ActionResult<TodoItem>> CreateTodoItem(TodoItem todoItem)
    {
        using var activity = _activitySource.StartActivity(
            "CreateTodoItem",
            ActivityKind.Server,
            Activity.Current?.Context ?? default);

        try
        {
            activity?.SetTag("todo.title", todoItem.Title);
            
            _context.TodoItems.Add(todoItem);
            await _context.SaveChangesAsync();

            activity?.SetTag("todo.id", todoItem.Id);
            
            return CreatedAtAction(
                nameof(GetTodoItem),
                new { id = todoItem.Id },
                todoItem);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            throw;
        }
    }
}
```

### 2. エラーハンドリングの計装

```csharp
public class TodoOperationException : Exception
{
    public string OperationType { get; }
    public long? ItemId { get; }

    public TodoOperationException(string operationType, long? itemId, string message, Exception? inner = null)
        : base(message, inner)
    {
        OperationType = operationType;
        ItemId = itemId;
    }
}

// コントローラーでの使用
[HttpPut("{id}")]
public async Task<IActionResult> UpdateTodoItem(long id, TodoItem todoItem)
{
    using var activity = _activitySource.StartActivity("UpdateTodoItem");
    activity?.SetTag("todo.id", id);
    
    try
    {
        if (id != todoItem.Id)
        {
            var ex = new TodoOperationException(
                "Update",
                id,
                "ID mismatch between route and body");
                
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            return BadRequest();
        }

        _context.Entry(todoItem).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return NoContent();
    }
    catch (DbUpdateConcurrencyException ex)
    {
        if (!TodoItemExists(id))
        {
            var notFoundEx = new TodoOperationException(
                "Update",
                id,
                "Item not found",
                ex);
                
            activity?.SetStatus(ActivityStatusCode.Error, notFoundEx.Message);
            activity?.RecordException(notFoundEx);
            return NotFound();
        }
        
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        activity?.RecordException(ex);
        throw;
    }
}
```

## メトリクスの実装

### 1. メーターの設定

```csharp
using System.Diagnostics.Metrics;

public static class Telemetry
{
    public static readonly ActivitySource ActivitySource = new("TodoApi.Activities");
    public static readonly Meter Meter = new("TodoApi.Metrics");
}
```

### 2. カウンターとヒストグラムの実装

```csharp
public class TodoMetrics
{
    private readonly Counter<long> _todoItemsCreated;
    private readonly Counter<long> _todoItemsCompleted;
    private readonly Histogram<double> _operationDuration;

    public TodoMetrics()
    {
        var meter = Telemetry.Meter;
        
        _todoItemsCreated = meter.CreateCounter<long>(
            "todo.items.created",
            description: "Number of todo items created");
            
        _todoItemsCompleted = meter.CreateCounter<long>(
            "todo.items.completed",
            description: "Number of todo items marked as complete");
            
        _operationDuration = meter.CreateHistogram<double>(
            "todo.operation.duration",
            unit: "ms",
            description: "Duration of todo operations");
    }

    public void RecordItemCreated()
    {
        _todoItemsCreated.Add(1);
    }

    public void RecordItemCompleted()
    {
        _todoItemsCompleted.Add(1);
    }

    public void RecordOperationDuration(string operation, double milliseconds)
    {
        _operationDuration.Record(
            milliseconds,
            new("operation", operation));
    }
}

// コントローラーでの使用
public class TodoItemsController : ControllerBase
{
    private readonly TodoMetrics _metrics;

    public TodoItemsController(TodoMetrics metrics)
    {
        _metrics = metrics;
    }

    [HttpPost]
    public async Task<ActionResult<TodoItem>> CreateTodoItem(TodoItem todoItem)
    {
        var sw = Stopwatch.StartNew();
        
        using var activity = _activitySource.StartActivity("CreateTodoItem");
        
        try
        {
            var result = await CreateTodoItemInternal(todoItem);
            
            _metrics.RecordItemCreated();
            _metrics.RecordOperationDuration("create", sw.ElapsedMilliseconds);
            
            return result;
        }
        catch
        {
            _metrics.RecordOperationDuration("create_failed", sw.ElapsedMilliseconds);
            throw;
        }
    }
}
```

## バッグトレースの連携

### 1. ロガーの設定

```csharp
public class TodoItemsController : ControllerBase
{
    private readonly ILogger<TodoItemsController> _logger;

    public TodoItemsController(ILogger<TodoItemsController> logger)
    {
        _logger = logger;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TodoItem>> GetTodoItem(long id)
    {
        using var activity = _activitySource.StartActivity("GetTodoItem");
        
        try
        {
            _logger.LogInformation(
                "Getting todo item {ItemId}. TraceId: {TraceId}",
                id,
                activity?.Context.TraceId);

            var todoItem = await _context.TodoItems.FindAsync(id);

            if (todoItem == null)
            {
                _logger.LogWarning(
                    "Todo item {ItemId} not found. TraceId: {TraceId}",
                    id,
                    activity?.Context.TraceId);
                    
                return NotFound();
            }

            return todoItem;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error getting todo item {ItemId}. TraceId: {TraceId}",
                id,
                activity?.Context.TraceId);
                
            throw;
        }
    }
}
```

## パフォーマンス最適化

### 1. サンプリング戦略

```csharp
public class TodoSamplingProcessor : BaseProcessor<Activity>
{
    private readonly string[] _importantOperations = new[]
    {
        "CreateTodoItem",
        "DeleteTodoItem"
    };

    public override void OnStart(Activity activity)
    {
        // 重要な操作は必ずサンプリング
        if (_importantOperations.Contains(activity.OperationName))
        {
            activity.ActivityTraceFlags |= ActivityTraceFlags.Recorded;
        }
    }
}

// Program.csでの設定
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProvider => tracerProvider
        .AddProcessor<TodoSamplingProcessor>()
        .SetSampler(new ParentBasedSampler(
            new TraceIdRatioBasedSampler(0.1))));
```

### 2. バッチ処理の最適化

```csharp
public async Task<IActionResult> CompleteManyTodoItems(long[] ids)
{
    using var activity = _activitySource.StartActivity("CompleteManyTodoItems");
    activity?.SetTag("todo.item.count", ids.Length);
    
    var sw = Stopwatch.StartNew();

    try
    {
        // バッチ更新の実装
        var items = await _context.TodoItems
            .Where(t => ids.Contains(t.Id))
            .ToListAsync();

        foreach (var item in items)
        {
            item.IsComplete = true;
        }

        await _context.SaveChangesAsync();

        _metrics.RecordOperationDuration(
            "complete_many",
            sw.ElapsedMilliseconds);
            
        activity?.SetTag("todo.completed.count", items.Count);
        
        return Ok();
    }
    catch (Exception ex)
    {
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        activity?.RecordException(ex);
        
        _metrics.RecordOperationDuration(
            "complete_many_failed",
            sw.ElapsedMilliseconds);
            
        throw;
    }
}
```

## デバッグのコツ

### 1. アクティビティの確認

```csharp
// デバッグ用の拡張メソッド
public static class ActivityExtensions
{
    public static void DumpInfo(this Activity activity)
    {
        Console.WriteLine($"Activity: {activity.OperationName}");
        Console.WriteLine($"TraceId: {activity.TraceId}");
        Console.WriteLine($"SpanId: {activity.SpanId}");
        Console.WriteLine($"ParentSpanId: {activity.ParentSpanId}");
        Console.WriteLine("Tags:");
        foreach (var tag in activity.Tags)
        {
            Console.WriteLine($"  {tag.Key}: {tag.Value}");
        }
    }
}

// 使用例
[HttpGet]
public async Task<ActionResult<IEnumerable<TodoItem>>> GetTodoItems()
{
    using var activity = _activitySource.StartActivity("GetTodoItems");
    activity?.DumpInfo(); // デバッグ時に活用
    
    // 処理の実装
}
```

### 2. メトリクスの確認

```csharp
public class DebugMetricsExporter : BaseExporter<Metric>
{
    public override ExportResult Export(
        in Batch<Metric> batch)
    {
        foreach (var metric in batch)
        {
            Console.WriteLine($"Metric: {metric.Name}");
            
            if (metric.MetricType == MetricType.Counter)
            {
                foreach (var point in metric.GetMetricPoints())
                {
                    Console.WriteLine($"  Value: {point.GetValue()}");
                }
            }
        }
        return ExportResult.Success;
    }
}

// Program.csでの設定（開発環境のみ）
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<DebugMetricsExporter>();
}
```

## セキュリティ考慮事項

### 1. 機密情報の取り扱い

```csharp
public async Task<ActionResult<TodoItem>> CreateTodoItem(TodoItem todoItem)
{
    using var activity = _activitySource.StartActivity("CreateTodoItem");
    
    // 機密情報をマスク
    activity?.SetTag("todo.title", 
        todoItem.Title.Length > 10 
            ? todoItem.Title.Substring(0, 10) + "..." 
            : todoItem.Title);
            
    // 個人情報は記録しない
    // activity?.SetTag("todo.user", todoItem.UserEmail); // NG
    
    // ...処理の実装
}
```

### 2. エラー情報の制御

```csharp
public class SafeExceptionHandler
{
    public static string GetSafeErrorMessage(Exception ex)
    {
        // 本番環境では詳細なエラー情報を隠蔽
        return "An error occurred processing your request.";
    }

    public static void RecordException(Activity? activity, Exception ex)
    {
        if (activity == null) return;

        // エラーの種類は記録
        activity.SetTag("error.type", ex.GetType().Name);
        
        // スタックトレースは開発環境のみ記録
        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") 
            == "Development")
        {
            activity.SetTag("error.stack", ex.StackTrace);
        }
    }
}
