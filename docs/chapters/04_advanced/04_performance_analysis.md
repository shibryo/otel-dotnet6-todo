# パフォーマンス分析

## 概要

本セクションでは、OpenTelemetryを使用した高度なパフォーマンス分析について学びます。パフォーマンスメトリクスの設計、ボトルネック検出、リソース使用率の監視、最適化指標の設定など、システムの性能を総合的に分析・改善する方法を解説します。

## パフォーマンスメトリクスの設計

### 1. レイテンシメトリクス

```csharp
public class LatencyMetrics
{
    private readonly Meter _meter;
    private readonly Histogram<double> _apiLatency;
    private readonly Histogram<double> _dbLatency;

    public LatencyMetrics()
    {
        _meter = new Meter("TodoApi.Performance");
        
        _apiLatency = _meter.CreateHistogram<double>(
            "api_latency",
            unit: "ms",
            description: "API endpoint latency");
            
        _dbLatency = _meter.CreateHistogram<double>(
            "db_operation_latency",
            unit: "ms",
            description: "Database operation latency");
    }

    public void RecordApiLatency(string endpoint, double milliseconds)
    {
        _apiLatency.Record(
            milliseconds,
            new KeyValuePair<string, object>("endpoint", endpoint));
    }

    public void RecordDbLatency(string operation, double milliseconds)
    {
        _dbLatency.Record(
            milliseconds,
            new KeyValuePair<string, object>("operation", operation));
    }
}
```

### 2. スループットメトリクス

```csharp
public class ThroughputMetrics
{
    private readonly Meter _meter;
    private readonly Counter<long> _requestCount;
    private readonly Counter<long> _dataOperations;

    public ThroughputMetrics()
    {
        _meter = new Meter("TodoApi.Throughput");
        
        _requestCount = _meter.CreateCounter<long>(
            "request_count",
            description: "Number of API requests");
            
        _dataOperations = _meter.CreateCounter<long>(
            "data_operations",
            description: "Number of database operations");
    }

    public void RecordRequest(string endpoint, string method)
    {
        _requestCount.Add(1, new KeyValuePair<string, object>[] 
        {
            new("endpoint", endpoint),
            new("method", method)
        });
    }

    public void RecordDataOperation(string type)
    {
        _dataOperations.Add(1, 
            new KeyValuePair<string, object>("type", type));
    }
}
```

## ボトルネック検出

### 1. パフォーマンストレース分析

```csharp
public class PerformanceTracer
{
    private readonly ActivitySource _activitySource;

    public PerformanceTracer()
    {
        _activitySource = new ActivitySource("TodoApi.Performance");
    }

    public async Task<T> TraceOperation<T>(
        string operationName,
        Func<Task<T>> operation)
    {
        using var activity = _activitySource.StartActivity(
            operationName,
            ActivityKind.Internal);

        var sw = Stopwatch.StartNew();
        try
        {
            var result = await operation();
            sw.Stop();

            activity?.SetTag("duration_ms", sw.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            activity?.SetTag("error", true);
            activity?.SetTag("error.message", ex.Message);
            throw;
        }
    }
}
```

### 2. ホットパス分析

```csharp
public class HotPathAnalyzer
{
    private readonly ILogger<HotPathAnalyzer> _logger;
    private readonly Histogram<double> _operationDuration;
    private readonly ConcurrentDictionary<string, long> _operationCounts;

    public HotPathAnalyzer(ILogger<HotPathAnalyzer> logger)
    {
        _logger = logger;
        var meter = new Meter("TodoApi.HotPath");
        _operationDuration = meter.CreateHistogram<double>(
            "operation_duration",
            unit: "ms");
        _operationCounts = new ConcurrentDictionary<string, long>();
    }

    public void RecordOperation(string path, double duration)
    {
        _operationDuration.Record(
            duration,
            new KeyValuePair<string, object>("path", path));

        _operationCounts.AddOrUpdate(
            path,
            1,
            (_, count) => count + 1);
    }

    public void AnalyzeHotPaths()
    {
        var hotPaths = _operationCounts
            .OrderByDescending(kv => kv.Value)
            .Take(5);

        foreach (var path in hotPaths)
        {
            _logger.LogInformation(
                "Hot path detected: {Path} with {Count} calls",
                path.Key,
                path.Value);
        }
    }
}
```

## リソース使用率の監視

### 1. システムリソースメトリクス

```csharp
public class SystemMetrics
{
    private readonly Meter _meter;
    private readonly ObservableGauge<double> _cpuUsage;
    private readonly ObservableGauge<double> _memoryUsage;
    private readonly ObservableGauge<double> _threadCount;

    public SystemMetrics()
    {
        _meter = new Meter("TodoApi.System");

        _cpuUsage = _meter.CreateObservableGauge<double>(
            "cpu_usage_percent",
            GetCpuUsage,
            unit: "%");

        _memoryUsage = _meter.CreateObservableGauge<double>(
            "memory_usage_mb",
            GetMemoryUsage,
            unit: "MB");

        _threadCount = _meter.CreateObservableGauge<double>(
            "thread_count",
            GetThreadCount);
    }

    private IEnumerable<Measurement<double>> GetCpuUsage()
    {
        var cpu = new PerformanceCounter(
            "Processor",
            "% Processor Time",
            "_Total");
        yield return new Measurement<double>(cpu.NextValue());
    }

    private IEnumerable<Measurement<double>> GetMemoryUsage()
    {
        var process = Process.GetCurrentProcess();
        var memoryMB = process.WorkingSet64 / 1024.0 / 1024.0;
        yield return new Measurement<double>(memoryMB);
    }

    private IEnumerable<Measurement<double>> GetThreadCount()
    {
        var process = Process.GetCurrentProcess();
        yield return new Measurement<double>(process.Threads.Count);
    }
}
```

### 2. リソース使用率アラート

```yaml
groups:
  - name: resource_alerts
    rules:
      # CPU使用率アラート
      - alert: HighCpuUsage
        expr: cpu_usage_percent > 80
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: High CPU usage detected

      # メモリ使用率アラート
      - alert: HighMemoryUsage
        expr: memory_usage_mb / total_memory_mb * 100 > 85
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: High memory usage detected

      # スレッド数アラート
      - alert: HighThreadCount
        expr: thread_count > 200
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: High thread count detected
```

## パフォーマンス改善指標

### 1. SLO（Service Level Objective）の設定

```csharp
public class PerformanceSLO
{
    private readonly Meter _meter;
    private readonly Histogram<double> _sloLatency;
    private readonly Counter<long> _sloViolations;

    public PerformanceSLO()
    {
        _meter = new Meter("TodoApi.SLO");
        
        _sloLatency = _meter.CreateHistogram<double>(
            "slo_latency",
            unit: "ms",
            description: "Latency for SLO tracking");
            
        _sloViolations = _meter.CreateCounter<long>(
            "slo_violations",
            description: "Number of SLO violations");
    }

    public void CheckSLO(string endpoint, double latency)
    {
        _sloLatency.Record(latency);

        // SLO: 99%のリクエストが500ms以内
        if (latency > 500)
        {
            _sloViolations.Add(1, new KeyValuePair<string, object>(
                "endpoint", endpoint));
        }
    }
}
```

### 2. パフォーマンスダッシュボード

```json
{
  "dashboard": {
    "panels": [
      {
        "title": "API Latency Distribution",
        "type": "heatmap",
        "datasource": "Prometheus",
        "targets": [
          {
            "expr": "rate(api_latency_bucket[5m])",
            "format": "heatmap"
          }
        ]
      },
      {
        "title": "Resource Usage",
        "type": "graph",
        "datasource": "Prometheus",
        "targets": [
          {
            "expr": "cpu_usage_percent",
            "legendFormat": "CPU Usage"
          },
          {
            "expr": "memory_usage_mb",
            "legendFormat": "Memory Usage (MB)"
          }
        ]
      },
      {
        "title": "SLO Compliance",
        "type": "gauge",
        "datasource": "Prometheus",
        "targets": [
          {
            "expr": "1 - rate(slo_violations[1h]) / rate(request_count[1h])",
            "legendFormat": "SLO Compliance"
          }
        ]
      }
    ]
  }
}
```

## ベストプラクティス

### 1. パフォーマンス監視

1. 多層的な監視
   - インフラストラクチャレベル
   - アプリケーションレベル
   - ビジネスレベル

2. メトリクス設計
   - 適切な粒度の設定
   - 重要な指標の選定
   - 相関分析の考慮

3. アラート設定
   - 誤検知の最小化
   - 段階的な通知
   - アクション可能な情報

### 2. パフォーマンス最適化

1. ボトルネック対策
   - 早期検出
   - 影響度の評価
   - 優先順位付け

2. リソース管理
   - キャパシティプランニング
   - スケーリング戦略
   - リソース使用効率

3. 継続的な改善
   - ベースライン測定
   - 定期的な評価
   - フィードバックループ

## トラブルシューティング

1. パフォーマンス問題の特定
   - メトリクスの相関分析
   - トレースの詳細分析
   - ボトルネックの特定

2. 改善策の実施
   - 段階的な適用
   - 影響の測定
   - 結果の検証
