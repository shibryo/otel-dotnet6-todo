# エラー検出と分析

## 概要

本セクションでは、OpenTelemetryを使用した高度なエラー検出と分析手法について学びます。エラーパターンの分類、コンテキスト情報の収集、トレースとの連携など、効果的なエラー監視と分析の実装方法を解説します。

## エラーパターンの分類

### 1. エラータイプの定義

```csharp
public enum ErrorType
{
    Validation,     // バリデーションエラー
    Business,       // ビジネスロジックエラー
    DataAccess,     // データアクセスエラー
    Integration,    // 外部サービス連携エラー
    System          // システムエラー
}

public class TodoError
{
    public ErrorType Type { get; set; }
    public string Code { get; set; }
    public string Message { get; set; }
    public Dictionary<string, string> Context { get; set; }
}
```

### 2. エラーハンドリングの実装

```csharp
public class ErrorHandler
{
    private readonly ILogger<ErrorHandler> _logger;
    private readonly ActivitySource _activitySource;
    private readonly TodoMetrics _metrics;

    public ErrorHandler(
        ILogger<ErrorHandler> logger,
        ActivitySource activitySource,
        TodoMetrics metrics)
    {
        _logger = logger;
        _activitySource = activitySource;
        _metrics = metrics;
    }

    public void HandleError(TodoError error, Activity currentActivity)
    {
        // エラー情報をトレースに記録
        currentActivity?.SetTag("error", true);
        currentActivity?.SetTag("error.type", error.Type.ToString());
        currentActivity?.SetTag("error.code", error.Code);

        // コンテキスト情報の追加
        foreach (var (key, value) in error.Context)
        {
            currentActivity?.SetTag($"error.context.{key}", value);
        }

        // メトリクスの記録
        _metrics.RecordError(error.Type);

        // ログの記録
        _logger.LogError(
            "Error occurred: Type={ErrorType}, Code={ErrorCode}, Message={Message}",
            error.Type,
            error.Code,
            error.Message);
    }
}
```

## エラーコンテキストの収集

### 1. コンテキストプロパゲーション

```csharp
public class ErrorContextPropagator
{
    private static readonly ActivitySource ActivitySource = 
        new("TodoApi.ErrorHandling");

    public static Activity StartErrorContext(TodoError error)
    {
        var activity = ActivitySource.StartActivity(
            "ErrorHandling",
            ActivityKind.Internal);

        if (activity != null)
        {
            // エラー情報の記録
            activity.SetTag("error.type", error.Type.ToString());
            activity.SetTag("error.code", error.Code);
            activity.SetTag("error.message", error.Message);

            // スタックトレースの記録
            var stackTrace = new StackTrace(true);
            activity.SetTag("error.stack", stackTrace.ToString());

            // タイムスタンプの記録
            activity.SetTag("error.timestamp", 
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        }

        return activity;
    }
}
```

### 2. エラーコンテキストの拡張

```csharp
public static class ErrorContextExtensions
{
    public static Activity AddHttpContext(
        this Activity activity,
        HttpContext context)
    {
        if (activity == null) return null;

        activity.SetTag("http.method", context.Request.Method);
        activity.SetTag("http.url", context.Request.Path);
        activity.SetTag("http.status_code", context.Response.StatusCode);
        activity.SetTag("http.client_ip", context.Connection.RemoteIpAddress);

        return activity;
    }

    public static Activity AddDatabaseContext(
        this Activity activity,
        Exception dbException)
    {
        if (activity == null) return null;

        if (dbException is DbException sqlEx)
        {
            activity.SetTag("db.error_code", sqlEx.ErrorCode);
            activity.SetTag("db.error_state", sqlEx.State);
        }

        return activity;
    }
}
```

## エラー分析とレポート

### 1. エラー集計とメトリクス

```csharp
public class ErrorMetrics
{
    private readonly Meter _meter;
    private readonly Counter<long> _errorCounter;
    private readonly Histogram<double> _errorResolutionTime;

    public ErrorMetrics()
    {
        _meter = new Meter("TodoApi.Errors");
        
        _errorCounter = _meter.CreateCounter<long>(
            "error_count",
            description: "Number of errors by type");
            
        _errorResolutionTime = _meter.CreateHistogram<double>(
            "error_resolution_time",
            unit: "ms",
            description: "Time taken to resolve errors");
    }

    public void RecordError(
        ErrorType type,
        string code,
        double? resolutionTime = null)
    {
        _errorCounter.Add(1, new KeyValuePair<string, object>[] 
        {
            new("error_type", type.ToString()),
            new("error_code", code)
        });

        if (resolutionTime.HasValue)
        {
            _errorResolutionTime.Record(resolutionTime.Value);
        }
    }
}
```

### 2. エラー分析レポートの生成

```csharp
public class ErrorAnalysisReport
{
    public async Task<ErrorReport> GenerateReport(
        DateTime start,
        DateTime end)
    {
        var report = new ErrorReport
        {
            Period = new { Start = start, End = end },
            Summary = await GetErrorSummary(start, end),
            TopErrors = await GetTopErrors(start, end),
            TrendAnalysis = await GetErrorTrends(start, end),
            ImpactAnalysis = await GetErrorImpact(start, end)
        };

        return report;
    }

    private class ErrorReport
    {
        public object Period { get; set; }
        public ErrorSummary Summary { get; set; }
        public List<TopError> TopErrors { get; set; }
        public ErrorTrends TrendAnalysis { get; set; }
        public ErrorImpact ImpactAnalysis { get; set; }
    }
}
```

## エラー監視とアラート

### 1. エラーベースのアラート設定

```yaml
groups:
  - name: error_alerts
    rules:
      # 重大エラー率のアラート
      - alert: HighCriticalErrorRate
        expr: |
          sum(rate(error_count{error_type="System"}[5m])) 
          / 
          sum(rate(error_count[5m])) > 0.1
        for: 5m
        labels:
          severity: critical
        annotations:
          summary: High rate of system errors detected

      # エラー解決時間のアラート
      - alert: LongErrorResolutionTime
        expr: |
          histogram_quantile(0.95, rate(error_resolution_time_bucket[15m])) 
          > 3600
        for: 15m
        labels:
          severity: warning
        annotations:
          summary: Long error resolution times detected
```

### 2. エラーダッシュボード設定

```json
{
  "dashboard": {
    "panels": [
      {
        "title": "Error Rate by Type",
        "type": "graph",
        "datasource": "Prometheus",
        "targets": [
          {
            "expr": "rate(error_count[5m])",
            "legendFormat": "{{error_type}}"
          }
        ]
      },
      {
        "title": "Error Resolution Time",
        "type": "heatmap",
        "datasource": "Prometheus",
        "targets": [
          {
            "expr": "rate(error_resolution_time_bucket[5m])",
            "format": "heatmap"
          }
        ]
      }
    ]
  }
}
```

## ベストプラクティス

### 1. エラー検出

1. 階層的なエラー処理
   - グローバルエラーハンドラー
   - コンポーネント固有のエラーハンドラー
   - エラーの伝播と集約

2. コンテキスト収集
   - 必要十分な情報収集
   - センシティブ情報の除外
   - 構造化されたコンテキスト

3. パフォーマンスへの配慮
   - 軽量なエラーハンドリング
   - 非同期処理の活用
   - バッファリングとバッチ処理

### 2. エラー分析

1. 効果的な分類
   - 明確なエラータイプ定義
   - エラーコードの体系化
   - 重要度の適切な判断

2. トレンド分析
   - 時系列での傾向把握
   - パターンの識別
   - 相関関係の分析

3. インパクト評価
   - ユーザーへの影響度
   - システムへの影響度
   - ビジネスへの影響度

## トラブルシューティング

1. エラー検出の問題
   - ログレベルの見直し
   - コンテキスト情報の確認
   - 伝播設定の検証

2. 分析の問題
   - データ収集の確認
   - 集計ロジックの検証
   - レポート生成の最適化
