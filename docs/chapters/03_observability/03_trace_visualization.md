# Jaegerによるトレース可視化

実際にトレースを生成・確認しながら、分散トレーシングについて学んでいきましょう。

## 1. トレースの生成

まず、アプリケーションにリクエストを送信してトレースを生成します：

```bash
# Todoアイテムの作成
curl -X POST http://localhost:5000/api/todoitems \
  -H "Content-Type: application/json" \
  -d '{"name": "OpenTelemetryを学ぶ", "isComplete": false}'

# 全アイテムの取得
curl http://localhost:5000/api/todoitems
```

> 💡 トレースとは？
> - 1つのリクエストの処理過程を追跡したもの
> - 複数のSpan（処理単位）で構成
> - 処理時間や依存関係を可視化

## 2. Jaeger UIでの確認

1. ブラウザで http://localhost:16686 を開く
2. 左メニューで以下を選択：
   - Service: `todo-api`
   - Operation: `POST /api/todoitems`
   - Tags: 必要に応じて検索条件を追加

```mermaid
sequenceDiagram
    Client->>+API: POST /api/todoitems
    API->>+DB: INSERT
    DB-->>-API: Result
    API-->>-Client: Response
```

> 💡 なぜトレース可視化が必要か？
> - パフォーマンス問題の特定
> - エラーの伝播状況の確認
> - サービス間の依存関係の理解

## 3. トレース分析の実践

### 3.1 正常系トレースの確認

1. 成功パターンの生成：
```bash
# 複数のTodoアイテムを作成
for i in {1..5}; do
  curl -X POST http://localhost:5000/api/todoitems \
    -H "Content-Type: application/json" \
    -d "{\"name\": \"タスク$i\", \"isComplete\": false}"
done
```

2. Jaeger UIで確認：
- [ ] 全体の処理時間
- [ ] 各Spanの所要時間
- [ ] Spanの親子関係
- [ ] タグ情報

### 3.2 エラーケースの確認

1. エラーパターンの生成：
```bash
# 存在しないIDのTodoを取得
curl http://localhost:5000/api/todoitems/999

# 不正なJSONでリクエスト
curl -X POST http://localhost:5000/api/todoitems \
  -H "Content-Type: application/json" \
  -d "{invalid json}"
```

2. エラートレースの分析：
- [ ] エラーフラグの確認
- [ ] エラーメッセージの内容
- [ ] スタックトレースの確認

## 4. 詳細分析の手法

### 4.1 Timeline View（タイムライン表示）

1. 処理時間の分布：
```mermaid
gantt
    title リクエスト処理のタイムライン
    HTTPリクエスト処理 :a1, 0, 2s
    バリデーション    :a2, after a1, 1s
    DBアクセス       :a3, after a2, 3s
    レスポンス生成   :a4, after a3, 1s
```

2. 確認ポイント：
- Spanの実行順序
- 処理の重なり
- 待機時間の発生箇所

### 4.2 Dependencies View（依存関係表示）

サービス間の呼び出し関係：
```mermaid
graph LR
    A[Client] -->|HTTP| B[Todo API]
    B -->|SQL| C[Database]
    B -->|Export| D[OTel Collector]
    D -->|Export| E[Jaeger]
```

## 5. パフォーマンス分析

### 5.1 ボトルネックの特定

1. 時間のかかる処理の特定：
```bash
# 大量のリクエストを送信
for i in {1..20}; do
  curl http://localhost:5000/api/todoitems &
done
wait
```

2. Jaeger UIで確認：
- 処理時間の長いSpan
- 同時実行時の遅延
- リソース競合の兆候

### 5.2 改善のヒント

- バッチ処理の検討
- キャッシュの活用
- 非同期処理の導入

## 6. トラブルシューティング

### 6.1 トレースが表示されない

1. 設定の確認：
```bash
# Collectorのログ確認
docker compose logs -f otelcol

# Jaegerのログ確認
docker compose logs -f jaeger
```

2. 環境変数の確認：
```bash
docker compose exec todo-api env | grep OTEL
```

### 6.2 トレース情報が不完全

1. Context伝搬の確認：
```http
# HTTPヘッダーの確認
GET /api/todoitems
traceparent: 00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01
```

2. サンプリング設定の確認：
```bash
# サンプリング率の確認
curl http://localhost:4317/sampling
```

次のセクションでは、PrometheusとGrafanaを使用したメトリクス監視を行います。
