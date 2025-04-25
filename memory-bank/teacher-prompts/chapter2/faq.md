# 第2章：OpenTelemetryに関するよくある質問と回答

## OpenTelemetryの基本概念について

### Q1: OpenTelemetryとは何ですか？
A: OpenTelemetryは、分散システムの可観測性を実現するためのオープンソースフレームワークです。

主な特徴：
1. 標準化された計装
   - 共通のAPIとデータモデル
   - マルチベンダー対応
   - 相互運用性の確保

2. 3つの主要コンポーネント
   - トレース（分散トレーシング）
   - メトリクス（数値データの収集）
   - ログ（構造化ログ）

### Q2: トレース、スパン、アクティビティの違いは何ですか？
A: それぞれの概念は以下のような関係です：

1. トレース
   - 一連の処理全体を表す
   - 複数のスパンで構成される
   - 一意のTraceIDを持つ

2. スパン
   - 処理の個々の部分を表す
   - 親子関係を持つことができる
   - スパンIDとタイムスタンプを持つ

3. アクティビティ
   - .NETにおけるスパンの実装
   - System.Diagnosticsの一部
   - OpenTelemetryと統合可能

## 実装に関する質問

### Q3: ActivitySourceの使い方がわかりません
A: ActivitySourceの基本的な使用方法：

1. ActivitySourceの作成
```csharp
// 静的クラスでの定義
public static class Telemetry
{
    public static readonly ActivitySource ActivitySource 
        = new("TodoApi.Activities");
}

// または、DI登録
services.AddSingleton(new ActivitySource("TodoApi.Activities"));
```

2. アクティビティの開始
```csharp
using var activity = _activitySource.StartActivity("OperationName");
```

3. 属性の設定
```csharp
activity?.SetTag("key", "value");
```

### Q4: メトリクスの実装方法がわかりません
A: メトリクスの基本的な実装手順：

1. メーターの作成
```csharp
var meter = new Meter("TodoApi.Metrics");
```

2. カウンターの使用
```csharp
// カウンターの作成
var counter = meter.CreateCounter<long>("todo.items.created");

// カウンターの増加
counter.Add(1);
```

3. ヒストグラムの使用
```csharp
// ヒストグラムの作成
var histogram = meter.CreateHistogram<double>(
    "todo.operation.duration",
    unit: "ms");

// 値の記録
histogram.Record(stopwatch.ElapsedMilliseconds);
```

### Q5: エラー情報の記録方法がわかりません
A: エラー情報の記録の基本パターン：

1. 例外情報の記録
```csharp
try
{
    // 処理
}
catch (Exception ex)
{
    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
    activity?.RecordException(ex);
    throw;
}
```

2. エラー属性の設定
```csharp
activity?.SetTag("error.type", ex.GetType().Name);
activity?.SetTag("error.message", ex.Message);
```

## トラブルシューティング

### Q6: トレースが表示されません
A: 以下の点を確認してください：

1. サンプラーの設定
```csharp
// 開発環境では全てのトレースを収集
builder.Services.AddOpenTelemetry()
    .WithTracing(builder => builder
        .SetSampler(new AlwaysOnSampler()));
```

2. ActivitySourceの登録
```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(builder => builder
        .AddSource("TodoApi.Activities"));
```

3. エクスポーターの設定
```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(builder => builder
        .AddConsoleExporter());  // デバッグ用
```

### Q7: メトリクスが収集されません
A: 以下の点を確認してください：

1. メーターの登録
```csharp
builder.Services.AddOpenTelemetry()
    .WithMetrics(builder => builder
        .AddMeter("TodoApi.Metrics"));
```

2. メトリクスの記録タイミング
```csharp
public async Task<IActionResult> CreateTodoItem(TodoItem item)
{
    // 操作の完了後にメトリクスを記録
    _todoItemsCreated.Add(1,
        new("status", "success"));
}
```

### Q8: パフォーマンスへの影響が心配です
A: 以下の最適化方法を検討してください：

1. サンプリングの調整
```csharp
// 本番環境では10%のサンプリング
builder.Services.AddOpenTelemetry()
    .WithTracing(builder => builder
        .SetSampler(new TraceIdRatioBasedSampler(0.1)));
```

2. バッチ処理の活用
```csharp
// メトリクスのバッチエクスポート
options.MetricReaderOptions.PeriodicExportingMetricReaderOptions
    .ExportIntervalMilliseconds = 5000;
```

## ベストプラクティス

### Q9: 適切なタグの付け方がわかりません
A: タグ付けのベストプラクティス：

1. 命名規則
```csharp
// 良い例
activity?.SetTag("todo.id", id);
activity?.SetTag("todo.operation", "create");

// 避けるべき例
activity?.SetTag("ID", id);  // 一貫性がない
activity?.SetTag("x", "y");  // 意味が不明確
```

2. 重要な情報の選択
```csharp
// 必要な情報
activity?.SetTag("todo.status", todoItem.IsComplete);
activity?.SetTag("todo.created_at", DateTime.UtcNow);

// 避けるべき情報（機密データ）
activity?.SetTag("todo.user_email", user.Email);  // NG
```

### Q10: 適切なメトリクスの選び方がわかりません
A: メトリクスの選定基準：

1. 基本的なメトリクス
```csharp
// 操作数のカウント
_todoItemsCreated.Add(1);
_todoItemsCompleted.Add(1);

// 処理時間の計測
_operationDuration.Record(elapsed.TotalMilliseconds);
```

2. ビジネスメトリクス
```csharp
// 完了率の計測
var completionRate = completedCount / totalCount;
_completionRateHistogram.Record(completionRate);

// エラー率の計測
_errorCounter.Add(1, new("error_type", ex.GetType().Name));
```

### Q11: 開発環境とプロダクション環境の設定の違いは？
A: 環境別の設定例：

1. 開発環境
```csharp
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddOpenTelemetry()
        .WithTracing(builder => builder
            .SetSampler(new AlwaysOnSampler())
            .AddConsoleExporter());
}
```

2. プロダクション環境
```csharp
if (builder.Environment.IsProduction())
{
    builder.Services.AddOpenTelemetry()
        .WithTracing(builder => builder
            .SetSampler(new TraceIdRatioBasedSampler(0.1))
            .AddOtlpExporter());
}
```

### Q12: デバッグ時の確認方法は？
A: デバッグ時の確認手順：

1. コンソール出力の活用
```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(builder => builder
        .AddConsoleExporter());
```

2. ログとの連携
```csharp
_logger.LogInformation(
    "Operation completed. TraceId: {TraceId}",
    Activity.Current?.TraceId.ToString());
```

3. デバッグビューの使用
```csharp
// デバッグ用の拡張メソッド
activity?.DumpInfo();  // アクティビティの詳細を表示
