# 第2章：OpenTelemetryの導入 レビュー基準

## レビュー基準

### 章全体の評価ポイント

1. 基本概念の理解
   - OpenTelemetryの役割と重要性
   - 分散トレーシングの仕組み
   - メトリクスとログの連携

2. 実装スキル
   - 適切なパッケージの選択
   - 基本設定の理解と実装
   - カスタム計装の応用

3. トラブルシューティング能力
   - エラー原因の特定
   - 適切な解決方法の選択
   - 予防的な実装の考慮

### セッション別の確認項目

1. SDK導入セッション
   ```checklist
   実装の確認：
   - [ ] 必要なパッケージが全て導入されている
   - [ ] Program.csで基本設定が適切に行われている
   - [ ] 自動計装が有効化されている

   理解度の確認：
   - [ ] 各パッケージの役割を説明できる
   - [ ] 設定項目の意味を理解している
   - [ ] トラブルシューティングの方法を知っている
   ```

2. トレース実装セッション
   - ActivitySourceの使用目的の確認
   - カスタムSpanの設計意図の確認
   - エラーハンドリングの考え方の共有

3. メトリクス実装セッション
   - メトリクス設計の意図確認
   - 計測項目の選定理由の確認
   - 実装方法の選択理由の共有

4. ログ連携セッション
   - ログ設計の考え方の確認
   - トレースとの連携方針の確認
   - エラー情報の取り扱いの共有

## チェックリスト

### 1. OpenTelemetry SDKのセットアップ
- [ ] 必要なパッケージのインストール
  - OpenTelemetry.Extensions.Hosting
  - OpenTelemetry.Instrumentation.AspNetCore
  - OpenTelemetry.Instrumentation.Http
  - OpenTelemetry.Instrumentation.EntityFrameworkCore
- [ ] Program.csでの基本設定
  - TracerProviderの設定
  - Resource設定
  - サンプラーの設定

### 2. 自動計装の設定
- [ ] ASP.NET Coreの自動計装
  - HTTPリクエスト/レスポンスの追跡
  - ミドルウェアの計装
- [ ] Entity Framework Coreの自動計装
  - データベース操作の追跡
  - クエリの実行時間計測
- [ ] HTTPクライアントの自動計装
  - 外部APIコール追跡
  - エラー処理の確認

### 3. カスタム計装の実装
- [ ] TodoItemsControllerのカスタムSpan
  - 各CRUDオペレーションの計装
  - 適切な属性の設定
  - エラー情報の記録
- [ ] ビジネスロジックの計装
  - 重要な処理のSpan作成
  - コンテキスト伝搬の実装
- [ ] バッチ処理の計装（該当する場合）
  - 一括操作の追跡
  - 処理時間の計測

### 4. メトリクス収集
- [ ] カスタムメトリクスの定義
  - Todo操作のカウンター
  - 処理時間のヒストグラム
  - 状態の計測
- [ ] メトリクスエクスポートの設定
  - エクスポート先の指定
  - バッチサイズの設定

### 5. エラー処理とロギング
- [ ] エラー時のSpan属性設定
  - エラータイプの記録
  - エラーメッセージの記録
  - スタックトレースの記録
- [ ] ログとトレースの連携
  - TraceIdの関連付け
  - ログレベルの適切な使用

## コード品質チェック

### 1. OpenTelemetry設定
```csharp
// 適切な設定例
services.AddOpenTelemetry()
    .WithTracing(builder => builder
        .AddAspNetCoreInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource("TodoApi")
        .SetResourceBuilder(ResourceBuilder.CreateDefault()
            .AddService("TodoApi"))
        .SetSampler(new AlwaysOnSampler()));
```

### 2. カスタムSpanの実装
```csharp
// 推奨パターン
public async Task<ActionResult<TodoItem>> CreateTodoItem(TodoItem todoItem)
{
    using var activity = _activitySource.StartActivity("CreateTodoItem");
    activity?.SetTag("todo.title", todoItem.Title);
    
    try
    {
        // 処理の実装
        return CreatedAtAction(nameof(GetTodoItem), new { id = todoItem.Id }, todoItem);
    }
    catch (Exception ex)
    {
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        throw;
    }
}
```

### 3. メトリクス実装
```csharp
// 推奨パターン
private readonly Counter<long> _todoItemsCreated;

public TodoItemsController()
{
    _todoItemsCreated = _meter.CreateCounter<long>("todo.items.created");
}

public async Task<ActionResult<TodoItem>> CreateTodoItem(TodoItem todoItem)
{
    // 処理の実装
    _todoItemsCreated.Add(1);
}
```

## 理解度確認質問

### 1. OpenTelemetryの基本概念
```
Q: トレース、スパン、アクティビティの関係を説明してください。
A: 
- トレースは一連の処理全体を表す
- スパンは処理の個々の部分を表す
- アクティビティは.NETでのスパンの実装
- トレースは複数のスパンで構成される
```

### 2. 自動計装
```
Q: 自動計装を使用する利点と注意点は？
A:
利点：
- 最小限のコードで基本的な計装が可能
- フレームワークレベルの処理を追跡
- 標準的なパターンの計装を省力化

注意点：
- カスタマイズの制限
- オーバーヘッドの考慮
- 必要な情報が不足する可能性
```

### 3. カスタム計装
```
Q: カスタム計装が必要なケースは？
A:
- ビジネスロジックの詳細な追跡
- 特定の処理のパフォーマンス計測
- カスタム属性の追加
- エラー情報の詳細な記録
```

## よくあるエラーと解決策

### 1. トレースが表示されない
```
考えられる原因：
1. サンプラーの設定が不適切
2. エクスポーターの設定ミス
3. コンテキスト伝搬の問題

解決策：
1. サンプラーの設定確認
2. エクスポーター設定の確認
3. コンテキスト伝搬の実装確認
```

### 2. メトリクスが収集されない
```
考えられる原因：
1. メーターの登録漏れ
2. エクスポート設定の問題
3. バッチ間隔の設定ミス

解決策：
1. メーター登録の確認
2. エクスポート設定の確認
3. バッチ間隔の適切な設定
```

## プログレスチェック

### 第2章の完了条件
1. OpenTelemetry SDK
- [ ] パッケージのインストール完了
- [ ] 基本設定の実装完了
- [ ] サンプラーの設定完了

2. 自動計装
- [ ] ASP.NET Core計装の確認
- [ ] EF Core計装の確認
- [ ] HTTP Client計装の確認

3. カスタム計装
- [ ] ActivitySourceの設定
- [ ] CRUD操作の計装
- [ ] エラーハンドリングの計装

4. メトリクス
- [ ] メーターの作成
- [ ] カウンターの実装
- [ ] ヒストグラムの実装

### 次のステップへの移行条件
1. すべてのチェックリストアイテムが完了
2. トレースの可視化確認
3. メトリクスの収集確認
4. エラーハンドリングの動作確認
