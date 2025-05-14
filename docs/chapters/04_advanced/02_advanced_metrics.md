# 高度なメトリクスとアラート

## 概要

本セクションでは、OpenTelemetryを使用した高度なメトリクス収集とアラート設定について学びます。ビジネスメトリクス、パフォーマンスメトリクス、カスタムメトリクスの実装と、効果的なアラート戦略の設計を行います。

## カスタムメトリクスの設計

### 1. メトリクスの種類と使い分け

```csharp
public class TodoMetrics
{
    private readonly Meter _meter;
    
    // カウンター：増加のみする値
    private readonly Counter<long> _todoCreated;
    
    // ヒストグラム：値の分布を記録
    private readonly Histogram<double> _completionTime;
    
    // アップダウンカウンター：増減する値
    private readonly UpDownCounter<long> _activeTodos;

    public TodoMetrics()
    {
        _meter = new Meter("TodoApi.Business");
        
        _todoCreated = _meter.CreateCounter<long>(
            "todo_items_created",
            description: "Number of created todo items");
            
        _completionTime = _meter.CreateHistogram<double>(
            "todo_completion_time",
            unit: "ms",
            description: "Time taken to complete todo items");
            
        _activeTodos = _meter.CreateUpDownCounter<long>(
            "active_todo_items",
            description: "Current number of active todo items");
    }
}
```

### 2. ビジネスメトリクスの実装

```csharp
public class BusinessMetrics
{
    private readonly Meter _meter;
    private readonly Dictionary<string, Counter<long>> _priorityCounters;
    
    public BusinessMetrics()
    {
        _meter = new Meter("TodoApi.Business");
        _priorityCounters = new Dictionary<string, Counter<long>>();
        
        // 優先度別の操作カウンター
        foreach (var priority in new[] { "high", "medium", "low" })
        {
            _priorityCounters[priority] = _meter.CreateCounter<long>(
                $"todo_operations_{priority}_priority",
                description: $"Operations count for {priority} priority items");
        }
    }
    
    public void RecordOperation(string priority, string operation)
    {
        if (_priorityCounters.TryGetValue(priority, out var counter))
        {
            counter.Add(1, new KeyValuePair<string, object>("operation", operation));
        }
    }
}
```

## パフォーマンスメトリクスの収集

### 1. レスポンスタイムの計測

```csharp
public class PerformanceMetrics
{
    private readonly Meter _meter;
    private readonly Histogram<double> _responseTime;
    
    public PerformanceMetrics()
    {
        _meter = new Meter("TodoApi.Performance");
        
        _responseTime = _meter.CreateHistogram<double>(
            "api_response_time",
            unit: "ms",
            description: "API response time");
    }
    
    public void RecordResponseTime(
        string endpoint,
        double milliseconds)
    {
        _responseTime.Record(
            milliseconds,
            new KeyValuePair<string, object>("endpoint", endpoint));
    }
}
```

### 2. リソース使用率の監視

```csharp
public class ResourceMetrics
{
    private readonly Meter _meter;
    private readonly ObservableGauge<double> _memoryUsage;
    private readonly ObservableGauge<double> _cpuUsage;
    
    public ResourceMetrics()
    {
        _meter = new Meter("TodoApi.Resources");
        
        _memoryUsage = _meter.CreateObservableGauge<double>(
            "memory_usage_mb",
            () => Process.GetCurrentProcess().WorkingSet64 / 1024.0 / 1024.0,
            unit: "MB",
            description: "Current memory usage");
            
        _cpuUsage = _meter.CreateObservableGauge<double>(
            "cpu_usage_percent",
            GetCpuUsage,
            unit: "%",
            description: "Current CPU usage");
    }
    
    private double GetCpuUsage()
    {
        // CPUリソース使用率を取得するロジック
        return 0.0;
    }
}
```

## アラート設定

### 1. プロメテウスルールの設定

```yaml
groups:
  - name: todo_alerts
    rules:
      # エラー率アラート
      - alert: HighErrorRate
        expr: rate(todo_api_errors_total[5m]) > 0.1
        for: 5m
        labels:
          severity: critical
        annotations:
          summary: High error rate detected
          
      # レスポンスタイムアラート
      - alert: SlowResponses
        expr: histogram_quantile(0.95, rate(api_response_time_bucket[5m])) > 500
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: Slow API responses detected
          
      # リソース使用率アラート
      - alert: HighMemoryUsage
        expr: memory_usage_mb > 1024
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: High memory usage detected
```

### 2. Grafanaアラートの設定

```typescript
// Grafana Alert Definition
{
  "name": "API Health Check",
  "type": "alerting",
  "conditions": [
    {
      "evaluator": {
        "params": [90],
        "type": "gt"
      },
      "operator": {
        "type": "and"
      },
      "query": {
        "params": ["A"]
      },
      "reducer": {
        "params": [],
        "type": "avg"
      },
      "type": "query"
    }
  ],
  "noDataState": "alerting",
  "executionErrorState": "alerting",
  "frequency": "1m",
  "handler": 1,
  "notifications": [
    {
      "uid": "slack-notification"
    }
  ]
}
```

## メトリクス可視化

### 1. Grafanaダッシュボード

```json
{
  "dashboard": {
    "panels": [
      {
        "title": "API Response Times",
        "type": "graph",
        "datasource": "Prometheus",
        "targets": [
          {
            "expr": "histogram_quantile(0.95, rate(api_response_time_bucket[5m]))",
            "legendFormat": "95th percentile"
          }
        ]
      },
      {
        "title": "Todo Operations by Priority",
        "type": "bar",
        "datasource": "Prometheus",
        "targets": [
          {
            "expr": "sum(rate(todo_operations_total[5m])) by (priority)",
            "legendFormat": "{{priority}}"
          }
        ]
      }
    ]
  }
}
```

## ベストプラクティス

### 1. メトリクス設計

1. 命名規則
   - 一貫性のある命名
   - メトリクスの目的を明確に
   - ラベルの適切な使用

2. カーディナリティ
   - 過度なラベル組み合わせを避ける
   - 時系列データの増加に注意
   - 重要な情報のみをラベル化

3. パフォーマンス
   - 収集頻度の最適化
   - バッチ処理の活用
   - リソース使用量の監視

### 2. アラート設計

1. 重要度の定義
   - Critical：即時対応が必要
   - Warning：監視が必要
   - Info：参考情報

2. アラートルール
   - 誤検知の最小化
   - 適切な閾値設定
   - 複合条件の活用

3. 通知設定
   - 適切な通知チャネル
   - エスカレーションルール
   - 抑制ルール

## トラブルシューティング

1. メトリクス収集の問題
   - メトリクス定義の確認
   - エクスポーターの動作確認
   - スクレイピング設定の確認

2. アラートの問題
   - ルール構文の検証
   - 閾値の妥当性確認
   - 通知設定の確認
