# 第4章：高度な機能と運用 - 実装アドバイス

## 章の学習目標

1. 運用視点での最適化
   - パフォーマンスチューニング
   - コスト効率の向上
   - スケーラビリティの確保

2. 高度な監視機能
   - カスタムメトリクスの設計
   - インテリジェントなサンプリング
   - 効果的なアラート設定

3. トラブルシューティングスキル
   - 問題の迅速な特定
   - 根本原因分析
   - 予防的対策

## セッション別コンテンツ

### 1. サンプリング最適化
参考資料：
- [サンプリング戦略](https://opentelemetry.io/docs/concepts/sampling/)
- [コスト最適化](https://opentelemetry.io/docs/instrumentation/net/using_sampling/)

実装例：
```csharp
public class AdvancedSamplingProcessor : BaseProcessor<Activity>
{
    private readonly HashSet<string> _criticalOperations = new()
    {
        "CreateOrder",
        "ProcessPayment"
    };

    public override void OnStart(Activity activity)
    {
        if (_criticalOperations.Contains(activity.OperationName))
        {
            // 重要な操作は常にサンプリング
            activity.ActivityTraceFlags |= ActivityTraceFlags.Recorded;
            return;
        }

        if (activity.GetTagItem("error") != null)
        {
            // エラー発生時は常にサンプリング
            activity.ActivityTraceFlags |= ActivityTraceFlags.Recorded;
            return;
        }

        // その他は負荷に応じて動的にサンプリング
        var currentLoad = GetSystemLoad();
        if (currentLoad > 0.8)
        {
            // 高負荷時は20%をサンプリング
            if (Random.Shared.NextDouble() <= 0.2)
            {
                activity.ActivityTraceFlags |= ActivityTraceFlags.Recorded;
            }
        }
        else
        {
            // 通常時は50%をサンプリング
            if (Random.Shared.NextDouble() <= 0.5)
            {
                activity.ActivityTraceFlags |= ActivityTraceFlags.Recorded;
            }
        }
    }
}
```

### 2. カスタムメトリクス
参考資料：
- [メトリクス設計パターン](https://prometheus.io/docs/practices/instrumentation/)
- [効果的な可視化](https://grafana.com/docs/grafana/latest/panels/visualizations/)

実装例：
```csharp
public class AdvancedMetrics
{
    private readonly Histogram<double> _operationLatency;
    private readonly Counter<long> _businessTransactions;
    private readonly ObservableGauge<int> _activeConnections;

    public AdvancedMetrics(Meter meter)
    {
        _operationLatency = meter.CreateHistogram<double>(
            "business.operation.latency",
            unit: "ms",
            description: "Business operation latency distribution");

        _businessTransactions = meter.CreateCounter<long>(
            "business.transactions",
            description: "Number of business transactions");

        _activeConnections = meter.CreateObservableGauge<int>(
            "system.active_connections",
            () => GetCurrentConnections());
    }

    public void RecordTransaction(string type, double latency)
    {
        _operationLatency.Record(latency, new("type", type));
        _businessTransactions.Add(1, new("type", type));
    }

    private int GetCurrentConnections()
    {
        // 実際の接続数を取得する実装
        return 0;
    }
}
```

### 3. エラー検知
参考資料：
- [アラート設計](https://prometheus.io/docs/practices/alerting/)
- [エラー分析パターン](https://opentelemetry.io/docs/instrumentation/net/manual/)

実装例：
```yaml
# prometheus-rules.yml
groups:
  - name: error_detection
    rules:
      - alert: HighErrorRate
        expr: |
          sum(rate(http_server_duration_count{status_code=~"5.."}[5m])) 
          / 
          sum(rate(http_server_duration_count[5m])) 
          > 0.05
        for: 5m
        labels:
          severity: critical
        annotations:
          summary: High error rate detected
          description: "Error rate is above 5% for the last 5 minutes"

      - alert: LatencySpike
        expr: |
          histogram_quantile(0.95, 
            rate(http_server_duration_bucket[5m])
          ) > 500
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: Latency spike detected
          description: "95th percentile latency is above 500ms"
```

### 4. パフォーマンス分析
参考資料：
- [パフォーマンス計測](https://learn.microsoft.com/ja-jp/dotnet/core/diagnostics/)
- [ボトルネック分析](https://opentelemetry.io/docs/instrumentation/net/resources/)

実装例：
```csharp
public class PerformanceAnalyzer
{
    private readonly AdvancedMetrics _metrics;
    private readonly ILogger<PerformanceAnalyzer> _logger;
    private readonly ActivitySource _activitySource;

    public async Task<T> MeasureOperation<T>(
        string operationName,
        Func<Task<T>> operation)
    {
        using var activity = _activitySource.StartActivity(operationName);
        var sw = Stopwatch.StartNew();

        try
        {
            var result = await operation();

            // パフォーマンスメトリクスの記録
            sw.Stop();
            _metrics.RecordTransaction(
                operationName,
                sw.ElapsedMilliseconds);

            // 詳細な情報をトレースに記録
            activity?.SetTag("operation.duration_ms", sw.ElapsedMilliseconds);
            activity?.SetTag("operation.success", true);

            if (sw.ElapsedMilliseconds > 1000)
            {
                // 遅いオペレーションを記録
                _logger.LogWarning(
                    "Slow operation detected: {Operation}, Duration: {Duration}ms",
                    operationName,
                    sw.ElapsedMilliseconds);
            }

            return result;
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

## デバッグとトラブルシューティング

### 1. 高度なトレース分析
```csharp
public static class TraceAnalyzer
{
    public static async Task AnalyzeTraces(
        IEnumerable<Activity> activities)
    {
        var criticalPaths = activities
            .Where(a => a.Duration > TimeSpan.FromSeconds(1))
            .OrderByDescending(a => a.Duration)
            .Take(10);

        foreach (var activity in criticalPaths)
        {
            Console.WriteLine($"Critical Path: {activity.OperationName}");
            Console.WriteLine($"Duration: {activity.Duration}");
            Console.WriteLine("Dependencies:");
            
            foreach (var child in activity.Children)
            {
                Console.WriteLine($"- {child.OperationName}: {child.Duration}");
            }
        }
    }
}
```

### 2. パフォーマンスプロファイリング
```csharp
public class Profiler
{
    private readonly ConcurrentDictionary<string, List<TimeSpan>> _timings
        = new();

    public void RecordTiming(string operation, TimeSpan duration)
    {
        _timings.AddOrUpdate(
            operation,
            new List<TimeSpan> { duration },
            (_, list) =>
            {
                list.Add(duration);
                return list;
            });
    }

    public void PrintStatistics()
    {
        foreach (var (operation, durations) in _timings)
        {
            var avg = durations.Average(d => d.TotalMilliseconds);
            var p95 = durations
                .OrderBy(d => d)
                .Skip((int)(durations.Count * 0.95))
                .First()
                .TotalMilliseconds;

            Console.WriteLine($"Operation: {operation}");
            Console.WriteLine($"Average: {avg:F2}ms");
            Console.WriteLine($"95th percentile: {p95:F2}ms");
        }
    }
}
```

## セキュリティ考慮事項

### 1. データ保護
```csharp
public static class SecurityUtils
{
    public static string MaskSensitiveData(string data)
    {
        if (string.IsNullOrEmpty(data)) return data;
        if (data.Length <= 4) return "****";
        return data.Substring(0, 2) + "****" + 
               data.Substring(data.Length - 2);
    }

    public static void ValidateMetricName(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Metric name cannot be empty");

        if (!Regex.IsMatch(name, "^[a-zA-Z_:][a-zA-Z0-9_:]*$"))
            throw new ArgumentException(
                "Invalid metric name. Use letters, numbers, underscores only");
    }
}
```

### 2. アクセス制御
```yaml
# grafana.ini
[auth]
disable_login_form = false
oauth_auto_login = false

[security]
admin_password = ${ADMIN_PASSWORD}
disable_initial_admin_creation = false
cookie_secure = true
strict_transport_security = true
```

## パフォーマンス最適化のベストプラクティス

1. バッチ処理
```csharp
public class BatchProcessor<T>
{
    private readonly List<T> _batch = new();
    private readonly int _batchSize;
    private readonly Func<IEnumerable<T>, Task> _processor;

    public async Task Add(T item)
    {
        _batch.Add(item);
        if (_batch.Count >= _batchSize)
        {
            await Flush();
        }
    }

    public async Task Flush()
    {
        if (_batch.Count > 0)
        {
            await _processor(_batch.ToList());
            _batch.Clear();
        }
    }
}
```

2. リソース管理
```csharp
public class ResourceManager : IDisposable
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks
        = new();
    private readonly ConcurrentDictionary<string, int> _usageCounts
        = new();

    public async Task<IDisposable> AcquireResource(
        string resourceId,
        int maxConcurrent = 1)
    {
        var semaphore = _locks.GetOrAdd(
            resourceId,
            _ => new SemaphoreSlim(maxConcurrent));

        await semaphore.WaitAsync();
        _usageCounts.AddOrUpdate(resourceId, 1, (_, count) => count + 1);

        return new ResourceHandle(this, resourceId);
    }

    public void ReleaseResource(string resourceId)
    {
        if (_locks.TryGetValue(resourceId, out var semaphore))
        {
            semaphore.Release();
            _usageCounts.AddOrUpdate(resourceId, 0, (_, count) => count - 1);
        }
    }

    public void Dispose()
    {
        foreach (var semaphore in _locks.Values)
        {
            semaphore.Dispose();
        }
    }
}
