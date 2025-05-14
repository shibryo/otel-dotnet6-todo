# サンプリング戦略の最適化

## 概要

本セクションでは、OpenTelemetryのサンプリング戦略を最適化し、効率的なトレース収集を実現する方法を学びます。環境に応じたサンプリング設定、重要度に基づく選択的サンプリング、エラー発生時の自動サンプリングなど、実践的なサンプリング戦略を実装します。

## サンプリングの種類と特徴

### 1. ヘッドベースサンプリング

```csharp
var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .SetSampler(new TraceIdRatioBaseSampler(0.1)) // 10%のサンプリング率
    .Build();
```

- 特徴
  - トレースの開始時に判断
  - シンプルで予測可能
  - システム全体で一貫した決定
- ユースケース
  - 基本的なトラフィック制御
  - リソース使用量の予測が必要な場合
  - 均一なサンプリングが望ましい場合

### 2. テールベースサンプリング

```csharp
public class ErrorBasedSampler : Sampler
{
    public override SamplingResult ShouldSample(
        SamplingParameters samplingParameters)
    {
        // エラーが発生した場合は必ずサンプリング
        var error = samplingParameters.Tags
            .FirstOrDefault(t => t.Key == "error");
        
        return error.Value != null
            ? new SamplingResult(SamplingDecision.RecordAndSample)
            : new SamplingResult(SamplingDecision.Drop);
    }
}
```

- 特徴
  - トレース完了後に判断
  - より詳細な情報に基づく決定
  - システムへの負荷が大きい
- ユースケース
  - エラー分析
  - パフォーマンス問題の調査
  - 重要な業務フローの監視

## 環境別サンプリング設定

### 開発環境

```csharp
public static IServiceCollection AddOpenTelemetryInDev(
    this IServiceCollection services)
{
    return services.AddOpenTelemetryTracing(builder =>
    {
        builder
            .SetSampler(new AlwaysOnSampler()) // 全てサンプリング
            .AddSource("TodoApi");
    });
}
```

### 本番環境

```csharp
public static IServiceCollection AddOpenTelemetryInProd(
    this IServiceCollection services)
{
    return services.AddOpenTelemetryTracing(builder =>
    {
        builder
            .SetSampler(new CompositeDelegate(
                // 基本は1%サンプリング
                new TraceIdRatioBaseSampler(0.01),
                // エラー時は100%サンプリング
                new ErrorBasedSampler()
            ))
            .AddSource("TodoApi");
    });
}
```

## 重要操作の優先サンプリング

### 優先度に基づくサンプリング実装

```csharp
public class PriorityBasedSampler : Sampler
{
    public override SamplingResult ShouldSample(
        SamplingParameters samplingParameters)
    {
        // 操作の優先度を確認
        var priority = samplingParameters.Tags
            .FirstOrDefault(t => t.Key == "priority");

        // 優先度に応じてサンプリング率を決定
        var samplingRate = priority.Value switch
        {
            "high" => 1.0,   // 高優先度は100%
            "medium" => 0.5,  // 中優先度は50%
            _ => 0.1         // その他は10%
        };

        return new Random().NextDouble() < samplingRate
            ? new SamplingResult(SamplingDecision.RecordAndSample)
            : new SamplingResult(SamplingDecision.Drop);
    }
}
```

### 優先度の設定例

```csharp
public class TodoItemsController : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<TodoItem>> PostTodoItem(TodoItem todoItem)
    {
        using var activity = _activitySource.StartActivity(
            "PostTodoItem",
            ActivityKind.Server,
            parentContext: default,
            new[] { new KeyValuePair<string, object>("priority", "high") }
        );

        // 以下、通常の処理
    }
}
```

## エラー時の自動サンプリング

### エラーハンドリングとサンプリング連携

```csharp
public class ErrorAwareSampler : Sampler
{
    private readonly ILogger<ErrorAwareSampler> _logger;
    private readonly double _defaultRate;

    public ErrorAwareSampler(
        ILogger<ErrorAwareSampler> logger,
        double defaultRate = 0.1)
    {
        _logger = logger;
        _defaultRate = defaultRate;
    }

    public override SamplingResult ShouldSample(
        SamplingParameters samplingParameters)
    {
        // エラー状態の確認
        var error = samplingParameters.Tags
            .FirstOrDefault(t => t.Key == "error");
        var statusCode = samplingParameters.Tags
            .FirstOrDefault(t => t.Key == "http.status_code");

        // エラー時は必ずサンプリング
        if (error.Value != null ||
            (statusCode.Value != null && 
             int.Parse(statusCode.Value.ToString()) >= 400))
        {
            _logger.LogInformation(
                "Error detected, forcing sampling");
            return new SamplingResult(
                SamplingDecision.RecordAndSample);
        }

        // 通常時は設定された率でサンプリング
        return new Random().NextDouble() < _defaultRate
            ? new SamplingResult(SamplingDecision.RecordAndSample)
            : new SamplingResult(SamplingDecision.Drop);
    }
}
```

## サンプリング戦略の検証

### パフォーマンス影響の測定

```csharp
public class SamplingMetrics
{
    private readonly Meter _meter;
    private readonly Counter<long> _sampledTraces;
    private readonly Counter<long> _totalTraces;

    public SamplingMetrics()
    {
        _meter = new Meter("TodoApi.Sampling");
        _sampledTraces = _meter.CreateCounter<long>(
            "sampled_traces",
            description: "Number of sampled traces");
        _totalTraces = _meter.CreateCounter<long>(
            "total_traces",
            description: "Total number of traces");
    }

    public void RecordSamplingDecision(bool sampled)
    {
        _totalTraces.Add(1);
        if (sampled)
        {
            _sampledTraces.Add(1);
        }
    }
}
```

### 戦略の評価と調整

1. サンプリング率の監視
   - メトリクスダッシュボードの作成
   - サンプリング決定の統計収集
   - リソース使用量との相関分析

2. 調整のポイント
   - システム負荷とサンプリング率の関係
   - 重要操作の捕捉率
   - エラー検出の確実性

## 実装のベストプラクティス

1. 段階的な導入
   - 基本サンプリングから開始
   - 監視と評価を行いながら最適化
   - 徐々に複雑な戦略を導入

2. 環境に応じた設定
   - 開発環境は詳細な情報収集
   - テスト環境でのパフォーマンス検証
   - 本番環境はリソースを考慮

3. モニタリングの重要性
   - サンプリング決定の統計収集
   - システムへの影響監視
   - 定期的な戦略の見直し
