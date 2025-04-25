# メトリクス監視とアラート設定

## 概要

この章では、OpenTelemetryを使用したメトリクス監視とアラート設定について説明します。カスタムメトリクスの実装、パフォーマンスメトリクスの収集、効果的なアラート設定の方法を学びます。

## 1. カスタムメトリクスの実装

### メトリクスの設計

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

### メトリクスの収集

1. ビジネスメトリクス
   ```csharp
   public void TodoCompleted(DateTime createdAt, string priority = "normal")
   {
       _todosCompletedCounter.Add(1);
       _activeTodosCounter.Add(-1);
       
       var completionTime = (DateTime.UtcNow - createdAt).TotalMilliseconds;
       _todoCompletionTimeHistogram.Record(completionTime);
   }
   ```

2. パフォーマンスメトリクス
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

3. エラーメトリクス
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

## 2. アラート設定

### Prometheusアラートルール

```yaml
groups:
- name: todo_alerts
  rules:
  # エラー率アラート
  - alert: HighErrorRate
    expr: rate(todo_operation_errors_total[5m]) > 0.1
    for: 5m
    labels:
      severity: warning
    annotations:
      summary: "高いエラー率を検出"
      description: "直近5分間のエラー率が10%を超えています"

  # レスポンスタイムアラート
  - alert: HighResponseTime
    expr: histogram_quantile(0.95, rate(todo_api_response_time_bucket[5m])) > 500
    for: 5m
    labels:
      severity: warning
    annotations:
      summary: "高いレスポンスタイムを検出"
      description: "p95レスポンスタイムが500msを超えています"

  # データベースエラーアラート
  - alert: DatabaseErrors
    expr: increase(todo_operation_errors_total{error_type="database_error"}[5m]) > 5
    for: 5m
    labels:
      severity: critical
    annotations:
      summary: "データベースエラーの増加"
      description: "5分間で5件以上のデータベースエラーが発生"
```

### モニタリングダッシュボード

```mermaid
graph TB
    A[パフォーマンス監視] --> B[レスポンスタイム]
    A --> C[エラー率]
    B --> D[API別レイテンシ]
    B --> E[DB操作時間]
    C --> F[エラータイプ別]
    C --> G[エンドポイント別]
```

## 3. パフォーマンス分析

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

## 4. パフォーマンス最適化のヒント

### データベース最適化

1. クエリパフォーマンス
   - インデックスの適切な使用
   - N+1問題の回避
   - クエリの実行計画確認

2. 接続管理
   - コネクションプールの設定
   - トランザクション範囲の最適化
   - デッドロック監視

### APIパフォーマンス

1. キャッシュ戦略
   - レスポンスキャッシュの活用
   - データキャッシュの設定
   - 分散キャッシュの検討

2. 非同期処理
   - 長時間処理の非同期化
   - バッチ処理の最適化
   - バックグラウンドジョブの活用

## まとめ

1. メトリクス実装
   - カスタムメトリクスの設計
   - 効果的なメトリクス収集
   - メトリクスの種類と使い分け

2. アラート設定
   - アラートルールの設計
   - 重要度の設定
   - 通知設定の最適化

3. パフォーマンス分析
   - レスポンスタイムの監視
   - リソース使用率の分析
   - 最適化の実践

## 次のステップ

- カスタムエクスポーターの実装
- 大規模システムでのメトリクス戦略
- アラート設定の高度な活用
- パフォーマンスチューニングの深掘り
