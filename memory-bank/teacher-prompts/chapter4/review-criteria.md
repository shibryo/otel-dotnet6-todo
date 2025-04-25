# 第4章：高度な機能と運用 レビュー基準

## レビューの目的
- サンプリング設定の適切な実装
- カスタムメトリクスの効果的な収集
- エラーハンドリングの改善
- パフォーマンス分析の実施

## チェックリスト

### 1. サンプリング設定
- [ ] 環境別サンプリング設定
  - 開発環境：100%サンプリング
  - 本番環境：10%サンプリング
- [ ] 重要操作の常時サンプリング
  - Create操作
  - Delete操作
  - エラー発生時
- [ ] カスタムサンプラーの実装
  - 優先度に基づく選択
  - エラー条件での自動サンプリング

### 2. カスタムメトリクス
- [ ] 基本メトリクスの実装
  - APIレスポンスタイム測定
  - エラー発生率の追跡
  - 操作種別ごとのカウント
- [ ] ヒストグラムの実装
  - 処理時間の分布
  - データサイズの分布
- [ ] カウンターの実装
  - 操作成功数
  - エラー発生数
  - リクエスト数

### 3. エラーハンドリング
- [ ] エラー種別の分類
  - データベースエラー
  - ビジネスロジックエラー
  - 外部サービスエラー
- [ ] エラー情報の記録
  - エラーコンテキスト
  - スタックトレース
  - 関連パラメータ
- [ ] エラーメトリクスの収集
  - エラー種別ごとの発生数
  - エラー発生時の処理時間
  - リトライ回数

### 4. パフォーマンス分析
- [ ] パフォーマンスメトリクス
  - レスポンスタイムの監視
  - リソース使用率の追跡
  - スループットの計測
- [ ] ボトルネック検出
  - 処理時間の異常値検出
  - リソース競合の特定
  - N+1問題の検出
- [ ] 最適化実装
  - キャッシュの活用
  - バッチ処理の最適化
  - クエリの最適化

## 理解度確認質問

1. サンプリング
```
Q: 異なる環境でサンプリング率を変える理由は？
A: - 開発環境：詳細なデバッグ情報が必要
   - 本番環境：パフォーマンスとコストの最適化
   - テスト環境：特定の機能の集中的な監視
```

2. メトリクス
```
Q: カウンターとヒストグラムの使い分けの基準は？
A: - カウンター：発生回数の記録（リクエスト数、エラー数）
   - ヒストグラム：値の分布の記録（レスポンスタイム、データサイズ）
```

3. エラーハンドリング
```
Q: エラー情報を記録する際の注意点は？
A: - 機密情報の除外
   - コンテキスト情報の適切な収集
   - エラーの重要度に応じた記録レベルの調整
```

## よくあるエラーと解決策

### 1. サンプリング関連
```
問題：重要な操作のトレースが欠落
解決：
1. サンプリング設定の見直し
2. 重要操作の明示的なフラグ設定
3. カスタムサンプラーの実装
```

### 2. メトリクス関連
```
問題：メトリクスの重複や欠落
解決：
1. メトリクス名の標準化
2. 収集タイミングの適正化
3. バッファサイズの調整
```

### 3. パフォーマンス関連
```
問題：予期せぬパフォーマンス低下
解決：
1. ボトルネック箇所の特定
2. リソース使用率の監視
3. クエリ最適化の実施
```

## 改善のアドバイス

### 1. サンプリング設定の最適化
```csharp
public class CustomSampler : Sampler
{
    private readonly double _defaultSamplingRate;
    private readonly HashSet<string> _importantOperations;

    public CustomSampler(double defaultSamplingRate)
    {
        _defaultSamplingRate = defaultSamplingRate;
        _importantOperations = new HashSet<string> { "Create", "Delete" };
    }

    public override SamplingResult ShouldSample(in SamplingParameters parameters)
    {
        // 重要な操作は常にサンプリング
        if (_importantOperations.Contains(parameters.Name))
        {
            return new SamplingResult(true);
        }

        // エラー時は常にサンプリング
        if (parameters.Tags.Any(t => t.Key == "error"))
        {
            return new SamplingResult(true);
        }

        // それ以外はデフォルトレート
        return new SamplingResult(Random.Shared.NextDouble() < _defaultSamplingRate);
    }
}
```

### 2. メトリクス収集の改善
```csharp
public class TodoMetrics
{
    private readonly Histogram<double> _operationDuration;
    private readonly Counter<long> _errorCount;
    private readonly Counter<long> _operationCount;

    public TodoMetrics(Meter meter)
    {
        _operationDuration = meter.CreateHistogram<double>(
            "todo.operation.duration",
            unit: "ms",
            description: "Duration of todo operations"
        );

        _errorCount = meter.CreateCounter<long>(
            "todo.errors",
            unit: "errors",
            description: "Number of errors occurred"
        );

        _operationCount = meter.CreateCounter<long>(
            "todo.operations",
            unit: "operations",
            description: "Number of operations performed"
        );
    }

    public void RecordOperation(string operation, double duration, bool isError = false)
    {
        _operationDuration.Record(duration, new("operation", operation));
        _operationCount.Add(1, new("operation", operation));
        
        if (isError)
        {
            _errorCount.Add(1, new("operation", operation));
        }
    }
}
```

## プログレスチェック

### 第4章の完了条件
1. サンプリング設定
- [ ] 環境別設定の実装
- [ ] 重要操作の設定
- [ ] エラー時の設定

2. メトリクス収集
- [ ] 基本メトリクスの実装
- [ ] カスタムメトリクスの実装
- [ ] 可視化の確認

3. エラーハンドリング
- [ ] エラー検出の実装
- [ ] エラー情報の記録
- [ ] メトリクスとの連携

4. パフォーマンス
- [ ] ボトルネック検出
- [ ] 最適化実装
- [ ] 効果測定

### 次のステップへの移行条件
1. すべてのチェックリストアイテムが完了
2. メトリクスの可視化確認
3. エラーハンドリングの動作確認
4. パフォーマンス改善の効果確認
