# 高度な監視と運用

## 概要

この章では、OpenTelemetryを使用した高度な監視と運用方法について説明します。サンプリング設定の最適化、カスタムメトリクスの効果的な実装、エラーハンドリング、そしてパフォーマンス分析の実践的な手法を学びます。

## 1. サンプリング設定の最適化

### カスタムサンプリングプロセッサの実装

```csharp
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

        // 重要なエンドポイントは常にサンプリング
        if (IsImportantEndpoint(activity))
        {
            activity.ActivityTraceFlags |= ActivityTraceFlags.Recorded;
            return;
        }

        // エラーが発生した場合は常にサンプリング
        if (HasError(activity))
        {
            activity.ActivityTraceFlags |= ActivityTraceFlags.Recorded;
            return;
        }

        // その他のケースではデフォルトのサンプリング比率を適用
        if (Random.Shared.NextDouble() < _defaultSamplingRatio)
        {
            activity.ActivityTraceFlags |= ActivityTraceFlags.Recorded;
        }
        else
        {
            activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
        }
    }
}
```

### サンプリング戦略のベストプラクティス

1. 環境に応じたサンプリング率の調整
   ```csharp
   services.AddOpenTelemetry()
       .WithTracing(builder =>
       {
           var samplingRatio = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development"
               ? 1.0  // 開発環境では100%サンプリング
               : 0.1; // 本番環境では10%サンプリング

           builder.AddProcessor(new TodoSamplingProcessor(samplingRatio));
       });
   ```

2. 重要度に基づくサンプリング
   - 重要な操作（作成・削除）は常にサンプリング
   - エラー発生時は常にサンプリング
   - その他の操作は設定された比率でサンプリング

3. パフォーマンスへの配慮
   - サンプリング判定の軽量化
   - キャッシュの活用
   - 早期リターンの実装

## 2. 高度なメトリクス実装

### カスタムメトリクスの設計

```csharp
public class TodoMetrics
{
    private readonly Counter<int> _todosCreatedCounter;
    private readonly Counter<int> _todosCompletedCounter;
    private readonly UpDownCounter<int> _activeTodosCounter;
    private readonly Histogram<double> _todoCompletionTimeHistogram;
    private readonly Counter<int> _todoOperationErrorCounter;
    private readonly Histogram<double> _apiResponseTimeHistogram;

    public TodoMetrics()
    {
        var meter = new Meter("TodoApi");
        
        // 基本的なメトリクス
        _todosCreatedCounter = meter.CreateCounter<int>("todo.created");
        _todosCompletedCounter = meter.CreateCounter<int>("todo.completed");
        _activeTodosCounter = meter.CreateUpDownCounter<int>("todo.active");
        
        // パフォーマンスメトリクス
        _todoCompletionTimeHistogram = meter.CreateHistogram<double>(
            "todo.completion_time",
            unit: "ms");
        _apiResponseTimeHistogram = meter.CreateHistogram<double>(
            "todo.api.response_time",
            unit: "ms");
        
        // エラーメトリクス
        _todoOperationErrorCounter = meter.CreateCounter<int>(
            "todo.operation.errors");
    }
}
```

### メトリクスの効果的な活用

1. ビジネスメトリクスの収集
   ```csharp
   public void TodoCompleted(DateTime createdAt, string priority = "normal")
   {
       _todosCompletedCounter.Add(1);
       _activeTodosCounter.Add(-1);
       
       var completionTime = (DateTime.UtcNow - createdAt).TotalMilliseconds;
       _todoCompletionTimeHistogram.Record(completionTime);
   }
   ```

2. パフォーマンスメトリクスの収集
   ```csharp
   public void RecordApiResponseTime(double milliseconds, string operation)
   {
       _apiResponseTimeHistogram.Record(milliseconds, 
           new KeyValuePair<string, object>[] 
           {
               new("operation", operation)
           });
   }
   ```

3. エラーメトリクスの収集
   ```csharp
   public void RecordOperationError(string operation, string errorType)
   {
       _todoOperationErrorCounter.Add(1, 
           new KeyValuePair<string, object>[] 
           {
               new("operation", operation),
               new("error_type", errorType)
           });
   }
   ```

## 3. エラーハンドリングの改善

### グローバルエラーハンドリング

```csharp
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly TodoMetrics _metrics;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var errorType = exception switch
        {
            ValidationException => "validation_error",
            DbUpdateException => "database_error",
            _ => "unknown_error"
        };

        _metrics.RecordOperationError(
            context.Request.Path,
            errorType);

        _logger.LogError(exception, 
            "Error processing request: {Path}",
            context.Request.Path);

        return true;
    }
}
```

### エラー監視とアラート設定

1. Prometheusアラートルール
   ```yaml
   groups:
   - name: todo_alerts
     rules:
     - alert: HighErrorRate
       expr: rate(todo_operation_errors_total[5m]) > 0.1
       for: 5m
       labels:
         severity: warning
       annotations:
         summary: "High error rate detected"
         description: "Error rate is above 10% for 5 minutes"
   ```

2. エラーパターンの分析
   ```sql
   SELECT
     error_type,
     COUNT(*) as error_count,
     AVG(duration) as avg_duration
   FROM todo_errors
   GROUP BY error_type
   ORDER BY error_count DESC
   LIMIT 10;
   ```

## 4. パフォーマンス分析

### レスポンスタイムの監視

1. p95/p99レイテンシの計測
   ```promql
   histogram_quantile(0.95, 
     sum(rate(todo_api_response_time_bucket[5m])) by (le, operation))
   ```

2. レイテンシのトレンド分析
   ```promql
   rate(todo_api_response_time_sum[1h]) / 
   rate(todo_api_response_time_count[1h])
   ```

### リソース使用率の監視

1. メモリ使用率
   ```promql
   process_working_set_bytes{job="todoapi"}
   ```

2. GCメトリクス
   ```promql
   dotnet_total_memory_bytes{job="todoapi"}
   ```

### パフォーマンス最適化のヒント

1. データベースクエリの最適化
   - インデックスの適切な使用
   - N+1問題の回避
   - クエリパフォーマンスの監視

2. キャッシュ戦略
   - 適切なキャッシュ期間の設定
   - キャッシュヒット率の監視
   - メモリ使用量の管理

## まとめ

1. サンプリング設定
   - 環境に応じた適切なサンプリング率
   - 重要な操作の優先サンプリング
   - パフォーマンスへの配慮

2. メトリクス実装
   - ビジネスメトリクスの収集
   - パフォーマンスメトリクスの監視
   - エラーメトリクスの追跡

3. エラーハンドリング
   - グローバルエラーハンドリング
   - エラーパターンの分析
   - アラート設定

4. パフォーマンス分析
   - レスポンスタイムの監視
   - リソース使用率の追跡
   - 最適化のベストプラクティス

## 次のステップ

- カスタムエクスポーターの実装
- 大規模システムでのトレース戦略
- アラート設定とインシデント管理
- パフォーマンスチューニング手法
