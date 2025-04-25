# メトリクスとログの実装

## 概要

この章では、OpenTelemetryを使用したメトリクスの収集と構造化ログの実装方法について説明します。Todoアプリケーションを例に、実践的なメトリクス設計とログ実装のパターンを学びます。

## メトリクスの実装

### 1. メトリクスの種類と使い分け

#### カウンター (Counter)
単調増加する値を記録します。

```csharp
public class TodoMetrics
{
    private readonly Counter<int> _todosCreatedCounter;
    
    public TodoMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("TodoApi");
        
        _todosCreatedCounter = meter.CreateCounter<int>(
            "todo.created_total",
            unit: "{todo}",
            description: "Total number of created todo items");
    }

    public void RecordTodoCreated()
    {
        _todosCreatedCounter.Add(1);
    }
}
```

#### アップダウンカウンター (UpDownCounter)
増減する値を記録します。

```csharp
public class TodoMetrics
{
    private readonly UpDownCounter<int> _activeTodosCounter;
    
    public TodoMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("TodoApi");
        
        _activeTodosCounter = meter.CreateUpDownCounter<int>(
            "todo.active_count",
            unit: "{todo}",
            description: "Current number of active todo items");
    }

    public void RecordTodoStatusChanged(bool wasActive, bool isActive)
    {
        if (!wasActive && isActive) _activeTodosCounter.Add(1);
        if (wasActive && !isActive) _activeTodosCounter.Add(-1);
    }
}
```

#### ヒストグラム (Histogram)
値の分布を記録します。

```csharp
public class TodoMetrics
{
    private readonly Histogram<double> _completionTimeHistogram;
    
    public TodoMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("TodoApi");
        
        _completionTimeHistogram = meter.CreateHistogram<double>(
            "todo.completion_time",
            unit: "ms",
            description: "Time taken to complete todo items");
    }

    public void RecordCompletionTime(DateTime createdAt)
    {
        var completionTime = (DateTime.UtcNow - createdAt).TotalMilliseconds;
        _completionTimeHistogram.Record(completionTime);
    }
}
```

### 2. メトリクス設計のベストプラクティス

#### 命名規則

```plaintext
[domain].[component]_[type]_[unit]
例：
- todo.items_created_total
- todo.items_active_count
- todo.completion_time_seconds
```

#### タグ（ラベル）の活用

```csharp
public class TodoMetrics
{
    private readonly Counter<int> _operationsCounter;
    
    public TodoMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("TodoApi");
        
        _operationsCounter = meter.CreateCounter<int>(
            "todo.operations_total",
            description: "Total number of todo operations");
    }

    public void RecordOperation(string operation, bool success)
    {
        var tags = new TagList
        {
            { "operation", operation },
            { "success", success.ToString() }
        };
        
        _operationsCounter.Add(1, tags);
    }
}
```

#### カスタムメトリクスの登録

```csharp
public static class MetricsConfiguration
{
    public static IServiceCollection AddCustomMetrics(
        this IServiceCollection services)
    {
        services.AddOpenTelemetry()
            .WithMetrics(builder => builder
                .AddMeter("TodoApi")  // カスタムメーターを登録
                .AddView("todo.completion_time", new ExplicitBucketHistogramConfiguration
                {
                    Boundaries = new[] { 100, 500, 1000, 2000, 5000 }  // ミリ秒単位のバケット
                }));

        return services;
    }
}
```

## ログの実装

### 1. 構造化ログの設定

#### ログプロバイダーの設定

```csharp
public static class LoggingConfiguration
{
    public static ILoggingBuilder AddCustomLogging(
        this ILoggingBuilder builder)
    {
        return builder
            .AddJsonConsole(options =>
            {
                options.IncludeScopes = true;
                options.TimestampFormat = "yyyy-MM-dd HH:mm:ss";
                options.JsonWriterOptions = new JsonWriterOptions
                {
                    Indented = true
                };
            });
    }
}
```

#### ログイベントの定義

```csharp
public static class LogEvents
{
    public static readonly EventId TodoCreated = new(1000, "TodoCreated");
    public static readonly EventId TodoCompleted = new(1001, "TodoCompleted");
    public static readonly EventId TodoDeleted = new(1002, "TodoDeleted");
    public static readonly EventId TodoOperationFailed = new(1003, "TodoOperationFailed");
}
```

### 2. コンテキスト情報の付加

#### ログスコープの利用

```csharp
public class TodoItemsController : ControllerBase
{
    private readonly ILogger<TodoItemsController> _logger;

    [HttpPost]
    public async Task<ActionResult<TodoItem>> PostTodoItem(TodoItem todoItem)
    {
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            ["UserId"] = User.Identity?.Name ?? "anonymous"
        }))
        {
            try
            {
                _logger.LogInformation(
                    LogEvents.TodoCreated,
                    "Creating todo item: {TodoTitle}",
                    todoItem.Title);

                // Todo作成処理
                return CreatedAtAction(nameof(GetTodoItem), new { id = todoItem.Id }, todoItem);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    LogEvents.TodoOperationFailed,
                    ex,
                    "Failed to create todo item: {TodoTitle}",
                    todoItem.Title);
                throw;
            }
        }
    }
}
```

### 3. 構造化ログの出力例

```json
{
  "Timestamp": "2025-04-25 17:14:31",
  "EventId": 1000,
  "EventName": "TodoCreated",
  "Level": "Information",
  "Message": "Creating todo item: Learn OpenTelemetry",
  "CorrelationId": "abcd1234...",
  "UserId": "user@example.com",
  "TodoTitle": "Learn OpenTelemetry",
  "TraceId": "efgh5678...",
  "SpanId": "ijkl9012..."
}
```

### 4. ログレベルの使い分け

```csharp
public class TodoService
{
    private readonly ILogger<TodoService> _logger;

    public async Task<TodoItem> CreateTodoItem(TodoItem item)
    {
        _logger.LogDebug("Validating todo item");  // 開発時の詳細情報

        _logger.LogInformation(
            "Creating todo item: {TodoTitle}",      // 運用監視に必要な情報
            item.Title);

        try
        {
            // Todo作成処理
            return item;
        }
        catch (Exception ex)
        {
            _logger.LogError(                      // エラー情報
                ex,
                "Failed to create todo item: {TodoTitle}",
                item.Title);
            throw;
        }
    }
}
```

## メトリクスとログの可視化

### 1. Grafanaダッシュボードの例

```plaintext
┌────────────────────┐  ┌────────────────────┐
│   Todo Creation    │  │   Active Todos     │
│   Rate            │  │   Count            │
│   [Graph]         │  │   [Gauge]          │
└────────────────────┘  └────────────────────┘
┌────────────────────┐  ┌────────────────────┐
│   Completion Time  │  │   Error Rate       │
│   Distribution     │  │   by Operation     │
│   [Heatmap]       │  │   [Bar Chart]      │
└────────────────────┘  └────────────────────┘
```

### 2. PromQLクエリ例

```plaintext
# 直近1時間のTodo作成レート（1分間隔）
rate(todo_created_total[1h])[1m]

# アクティブなTodoの数
todo_active_count

# 完了時間の90パーセンタイル
histogram_quantile(0.9, sum(rate(todo_completion_time_bucket[5m])) by (le))

# 操作タイプ別のエラー率
sum(rate(todo_operations_total{success="false"}[5m])) by (operation) /
sum(rate(todo_operations_total[5m])) by (operation)
```

## まとめ

1. メトリクス
   - 適切なメトリクスタイプの選択
   - 命名規則の統一
   - タグによる次元の追加
   - カスタムビューの設定

2. ログ
   - 構造化ログの活用
   - コンテキスト情報の付加
   - 適切なログレベルの使用
   - トレース情報との連携

3. 可視化
   - 効果的なダッシュボード設計
   - メトリクスの集計と分析
   - アラート設定への活用

## 次のステップ

次章では、これまでに実装した可観測性機能を活用して、アプリケーションの監視と運用について学んでいきます。
