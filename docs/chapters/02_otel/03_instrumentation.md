# OpenTelemetryによる計装

このセクションでは、TodoアプリケーションにOpenTelemetryを使用した計装（インスツルメンテーション）を実装します。

## ActivitySourceの設定

### 1. ActivitySourceの作成

```csharp
public class TodoItemsController : ControllerBase
{
    private static readonly ActivitySource _activitySource = 
        new("TodoApi");
}
```

> 💡 ActivitySourceとは
> - トレースを生成するための起点
> - アプリケーション名を指定
> - コンポーネント単位で作成可能

## トレースの実装

### 1. 基本的なトレース

```csharp
[HttpGet]
public async Task<ActionResult<IEnumerable<TodoItem>>> GetTodoItems()
{
    using var activity = _activitySource.StartActivity("GetTodoItems");
    var items = await _context.TodoItems.ToListAsync();
    activity?.SetTag("todo.count", items.Count);
    return items;
}
```

### 2. 詳細な情報の追加

```csharp
[HttpGet("{id}")]
public async Task<ActionResult<TodoItem>> GetTodoItem(int id)
{
    using var activity = _activitySource.StartActivity("GetTodoItem");
    activity?.SetTag("todo.id", id);

    var todoItem = await _context.TodoItems.FindAsync(id);

    if (todoItem == null)
    {
        activity?.SetStatus(ActivityStatusCode.Error, "Todo item not found");
        return NotFound();
    }

    activity?.SetTag("todo.title", todoItem.Title);
    activity?.SetTag("todo.is_complete", todoItem.IsComplete);
    return todoItem;
}
```

### 3. エラー処理の計装

```csharp
[HttpPost]
public async Task<ActionResult<TodoItem>> PostTodoItem(TodoItem todoItem)
{
    try
    {
        using var activity = _activitySource.StartActivity("CreateTodoItem");
        activity?.SetTag("todo.title", todoItem.Title);

        _context.TodoItems.Add(todoItem);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTodoItem), 
            new { id = todoItem.Id }, todoItem);
    }
    catch (Exception ex)
    {
        activity?.SetStatus(ActivityStatusCode.Error);
        activity?.SetTag("error.type", ex.GetType().Name);
        activity?.SetTag("error.message", ex.Message);
        throw;
    }
}
```

## パフォーマンス計測

### 1. 処理時間の計測

```csharp
[HttpPut("{id}")]
public async Task<IActionResult> PutTodoItem(int id, TodoItem todoItem)
{
    var stopwatch = Stopwatch.StartNew();
    try
    {
        using var activity = _activitySource.StartActivity("UpdateTodoItem");
        // 更新処理
        return NoContent();
    }
    finally
    {
        stopwatch.Stop();
        activity?.SetTag("duration_ms", stopwatch.ElapsedMilliseconds);
    }
}
```

### 2. カスタムメトリクスの記録

```csharp
public class TodoMetrics
{
    private readonly ActivitySource _activitySource;
    private readonly TodoMetrics _metrics;

    public TodoItemsController(TodoContext context, TodoMetrics metrics)
    {
        _metrics = metrics;
        _activitySource = new ActivitySource("TodoApi");
    }

    [HttpPost]
    public async Task<ActionResult<TodoItem>> PostTodoItem(TodoItem todoItem)
    {
        using var activity = _activitySource.StartActivity("CreateTodoItem");
        try
        {
            // Todo作成処理
            _metrics.TodoCreated(priority);
            return result;
        }
        catch (Exception ex)
        {
            _metrics.RecordOperationError("create", ex.GetType().Name);
            throw;
        }
    }
}
```

## 動作確認

### 1. トレースの生成

```bash
# サンプルリクエストの送信
curl -X POST http://localhost:5000/api/todoitems \
  -H "Content-Type: application/json" \
  -d '{"title":"OpenTelemetryのテスト","isComplete":false}'

# ログの確認
docker compose logs -f api | grep -i trace
```

### 2. メトリクスの確認

```bash
# メトリクスエンドポイントの確認
curl http://localhost:5000/metrics

# Prometheusターゲットの確認
curl http://localhost:9090/api/v1/targets
```

## トラブルシューティング

### 1. トレースが出力されない場合

```bash
# サンプリング設定の確認
docker compose exec api env | grep OTEL_TRACES

# ログレベルの確認
docker compose exec api env OTEL_LOG_LEVEL=debug

# Collectorとの接続確認
docker compose exec api nc -zv otelcol 4317
```

### 2. メトリクスが収集されない場合

```bash
# メトリクスエクスポートの確認
docker compose logs -f api | grep -i metrics

# Prometheusの接続確認
curl http://localhost:9090/api/v1/status/config
```

## 次のステップ

計装の実装が完了したら、[メトリクスとログの実装](./04_metrics_and_logging.md)に進みます。
