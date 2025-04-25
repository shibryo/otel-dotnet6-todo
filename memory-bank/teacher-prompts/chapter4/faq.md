# 第4章：高度な機能と運用 よくある質問と回答

## サンプリングについて

### Q1: サンプリング率をどのように決めればよいですか？
A: サンプリング率は以下の要素を考慮して決定します：

1. 環境による違い
   - 開発環境：100%（すべてのトレースを収集）
   - テスト環境：50%（一定量のトレースを収集）
   - 本番環境：10%（パフォーマンスとコストを考慮）

2. トラフィック量
   - 少量：より高いサンプリング率
   - 大量：より低いサンプリング率

3. 重要度による調整
   - 重要な操作：100%
   - エラー発生時：100%
   - 通常操作：環境に応じたレート

### Q2: カスタムサンプラーはいつ必要ですか？
A: 以下のような場合にカスタムサンプラーが必要です：

1. 操作の重要度に応じた制御が必要な場合
2. エラー発生時の確実な記録が必要な場合
3. 特定の条件での選択的なサンプリングが必要な場合
4. コストとパフォーマンスの最適化が必要な場合

## メトリクスについて

### Q3: どのようなメトリクスを収集すべきですか？
A: 以下のカテゴリのメトリクスを収集することを推奨します：

1. パフォーマンスメトリクス
   - レスポンスタイム
   - スループット
   - エラー率

2. ビジネスメトリクス
   - 操作種別ごとの実行回数
   - 成功/失敗率
   - データ量の推移

3. リソースメトリクス
   - メモリ使用量
   - CPU使用率
   - データベース接続数

### Q4: メトリクス収集のパフォーマンスへの影響が心配です
A: 以下の対策を検討してください：

1. バッファリングの活用
```csharp
private readonly BatchingOptions _options = new()
{
    BatchSize = 100,
    FlushInterval = TimeSpan.FromSeconds(10)
};
```

2. 適切なサンプリング
```csharp
public void RecordDuration(double duration)
{
    if (Random.Shared.NextDouble() < 0.1) // 10%のサンプリング
    {
        _histogram.Record(duration);
    }
}
```

3. 非同期処理の活用
```csharp
public async Task RecordMetricsAsync(MetricData data)
{
    await Task.Run(() => ProcessMetrics(data));
}
```

## エラーハンドリングについて

### Q5: どのようなエラー情報を記録すべきですか？
A: 以下の情報を記録することを推奨します：

1. 基本情報
   - エラーの種類
   - エラーメッセージ
   - 発生時刻

2. コンテキスト情報
   - TraceId
   - SpanId
   - 操作の種類

3. 環境情報
   - アプリケーションバージョン
   - 環境（開発/テスト/本番）
   - サーバー情報

### Q6: エラー情報の機密性が心配です
A: 以下の対策を実装してください：

1. エラーメッセージのサニタイズ
```csharp
public static string SanitizeErrorMessage(string message)
{
    // 機密情報のパターンを定義
    var patterns = new[]
    {
        @"password=\S+",
        @"token=\S+",
        @"key=\S+"
    };

    // パターンに一致する部分を置換
    foreach (var pattern in patterns)
    {
        message = Regex.Replace(message, pattern, "[REDACTED]");
    }

    return message;
}
```

2. 環境による制御
```csharp
public string GetErrorDetail(Exception ex)
{
    if (_env.IsDevelopment())
    {
        return ex.ToString(); // 開発環境では詳細を表示
    }

    return "An error occurred"; // 本番環境では一般的なメッセージ
}
```

## パフォーマンスについて

### Q7: パフォーマンス問題の特定方法は？
A: 以下の手順で特定します：

1. メトリクスの監視
```csharp
public class PerformanceMonitor
{
    private const double SlowOperationThreshold = 1000; // 1秒

    public void AnalyzePerformance(double duration)
    {
        if (duration > SlowOperationThreshold)
        {
            _logger.LogWarning(
                "Slow operation detected: {Duration}ms",
                duration);
        }
    }
}
```

2. トレースの分析
```csharp
public async Task AnalyzeTrace(string traceId)
{
    var spans = await _tracer.GetSpansAsync(traceId);
    var sortedSpans = spans.OrderByDescending(s => s.Duration);

    foreach (var span in sortedSpans.Take(5))
    {
        _logger.LogInformation(
            "Long running span: {Operation}, Duration: {Duration}ms",
            span.Operation,
            span.Duration);
    }
}
```

### Q8: N+1問題の検出と対策は？
A: 以下の方法で対応します：

1. 検出方法
```csharp
public class QueryCounter
{
    private int _queryCount;
    private readonly int _threshold = 10;

    public void OnQueryExecuted()
    {
        _queryCount++;
        if (_queryCount > _threshold)
        {
            _logger.LogWarning(
                "Possible N+1 problem detected: {QueryCount} queries executed",
                _queryCount);
        }
    }
}
```

2. 対策例
```csharp
// 問題のあるコード
public async Task<List<TodoItem>> GetItemsWithTags()
{
    var items = await _context.TodoItems.ToListAsync();
    foreach (var item in items)
    {
        item.Tags = await _context.Tags
            .Where(t => t.ItemId == item.Id)
            .ToListAsync();
    }
    return items;
}

// 改善後のコード
public async Task<List<TodoItem>> GetItemsWithTags()
{
    return await _context.TodoItems
        .Include(i => i.Tags)
        .ToListAsync();
}
```

## デバッグについて

### Q9: 本番環境でのデバッグ方法は？
A: 以下の方法を組み合わせて対応します：

1. 構造化ログの活用
```csharp
public void LogOperation(string operation, object context)
{
    _logger.LogInformation(
        "Operation {Operation} executed with context {@Context}",
        operation,
        context);
}
```

2. トレース情報の活用
```csharp
public async Task<IActionResult> ExecuteOperation()
{
    var activity = Activity.Current;
    
    try
    {
        // 操作の実行
        return Ok();
    }
    catch (Exception ex)
    {
        _logger.LogError(
            ex,
            "Error in operation. TraceId: {TraceId}",
            activity?.TraceId);
        throw;
    }
}
```

### Q10: パフォーマンス分析の方法は？
A: 以下のツールと方法を活用します：

1. メトリクス分析
```csharp
public class PerformanceAnalyzer
{
    public void AnalyzeMetrics(IEnumerable<MetricData> metrics)
    {
        var stats = metrics
            .GroupBy(m => m.Operation)
            .Select(g => new
            {
                Operation = g.Key,
                Average = g.Average(m => m.Duration),
                P95 = CalculatePercentile(g.Select(m => m.Duration), 95),
                P99 = CalculatePercentile(g.Select(m => m.Duration), 99)
            });

        foreach (var stat in stats)
        {
            _logger.LogInformation(
                "Operation: {Operation}, Avg: {Avg}ms, P95: {P95}ms, P99: {P99}ms",
                stat.Operation,
                stat.Average,
                stat.P95,
                stat.P99);
        }
    }
}
```

2. トレース分析
```csharp
public class TraceAnalyzer
{
    public async Task AnalyzeTraces(DateTime start, DateTime end)
    {
        var traces = await _tracer.GetTracesAsync(start, end);
        var slowTraces = traces
            .Where(t => t.Duration > TimeSpan.FromSeconds(1))
            .ToList();

        foreach (var trace in slowTraces)
        {
            _logger.LogWarning(
                "Slow trace detected: {TraceId}, Duration: {Duration}ms",
                trace.TraceId,
                trace.Duration.TotalMilliseconds);
        }
    }
}
