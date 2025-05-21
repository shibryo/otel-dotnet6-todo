# メトリクス監視の設定

実際にメトリクスを収集・可視化しながら、監視の仕組みについて学んでいきましょう。

## 1. Prometheusの設定

まず、メトリクス収集の設定を行います：

### 1.1 prometheus.ymlの作成

以下の内容で`prometheus.yml`を作成します：

```yaml
global:
  scrape_interval: 5s    # メトリクス収集の間隔

scrape_configs:
  - job_name: 'otel-collector'
    static_configs:
      - targets: ['otelcol:8889']
    metrics_path: '/metrics'

  - job_name: 'todo-api'
    static_configs:
      - targets: ['todo-api:5000']
    metrics_path: '/metrics/prometheus'
    scheme: 'http'
```

> 💡 収集間隔について
> - 短すぎると負荷が高くなる
> - 長すぎると変化を見逃す
> - 開発時は短め、本番は長めに設定

### 1.2 設定の適用

```bash
# Prometheusの再起動
docker compose restart prometheus

# 設定の確認
curl http://localhost:9090/api/v1/status/config
```

## 2. メトリクスの収集

### 2.1 サンプルデータの生成

```bash
# 正常系リクエスト
for i in {1..10}; do
  curl -X POST http://localhost:5000/api/todoitems \
    -H "Content-Type: application/json" \
    -d "{\"name\": \"タスク$i\", \"isComplete\": false}"
done

# エラー系リクエスト
curl http://localhost:5000/api/todoitems/999
```

### 2.2 Prometheusでの確認

1. ブラウザで http://localhost:9090 を開く
2. 以下のクエリを試してみましょう：

```promql
# リクエスト総数
http_server_requests_total

# 1分あたりのリクエスト数
rate(http_server_requests_total[1m])

# エラー率
sum(rate(http_server_errors_total[5m])) / 
sum(rate(http_server_requests_total[5m])) * 100
```

> 💡 なぜrate()を使うのか？
> - Counter型は単調増加のため、差分を見る必要がある
> - rate()で単位時間あたりの変化量を計算
> - 傾向の把握が容易になる

## 3. Grafanaでの可視化

### 3.1 データソースの追加

1. http://localhost:3000 にアクセス（初期認証情報：admin/admin）
2. Configuration → Data sources → Add data source
3. Prometheusを選択し、以下を設定：
   - URL: `http://prometheus:9090`
   - Access: Server

### 3.2 ダッシュボードの作成

1. 新規ダッシュボード作成
2. パネルの追加：

```bash
# リクエストレート
rate(http_server_requests_total[5m])

# レスポンスタイム
histogram_quantile(0.95, 
  rate(http_request_duration_seconds_bucket[5m]))

# エラー率
sum(rate(http_server_errors_total[5m])) / 
sum(rate(http_server_requests_total[5m])) * 100
```

> 💡 パネルの選び方
> - 時系列データ → グラフ
> - 現在値 → ゲージ
> - 分布 → ヒストグラム
> - 関係性 → ヒートマップ

### 3.3 アラートの設定

1. アラートルールの作成：
```yaml
# エラー率アラート
- alert: HighErrorRate
  expr: sum(rate(http_server_errors_total[5m])) / 
       sum(rate(http_server_requests_total[5m])) * 100 > 5
  for: 5m
  labels:
    severity: critical
  annotations:
    summary: "エラー率が高い"
```

2. 通知チャンネルの設定：
   - Alerting → Notification channels
   - Email, Slack等の設定

## 4. パフォーマンス分析

### 4.1 負荷テスト実行

```bash
# 連続リクエスト生成
for i in {1..100}; do
  curl http://localhost:5000/api/todoitems &
done
wait
```

### 4.2 メトリクス確認

1. レスポンスタイムの分布：
```promql
histogram_quantile(0.95, 
  rate(http_request_duration_seconds_bucket[5m]))
```

2. リソース使用状況：
```promql
# メモリ使用率
process_resident_memory_bytes{job="todo-api"}

# CPU使用率
rate(process_cpu_seconds_total{job="todo-api"}[5m])
```

## 5. トラブルシューティング

### 5.1 メトリクス収集の問題

1. スクレイプ設定の確認：
```bash
# ターゲットの状態確認
curl http://localhost:9090/api/v1/targets

# メトリクスの到達確認
curl http://localhost:5000/metrics/prometheus
```

2. ログの確認：
```bash
# Prometheusのログ
docker compose logs -f prometheus

# アプリケーションのログ
docker compose logs -f todo-api
```

### 5.2 Grafanaの問題

1. データソース接続：
```bash
# Prometheusの疎通確認
docker compose exec grafana wget -q -O- http://prometheus:9090/api/v1/status

# Grafanaのログ確認
docker compose logs -f grafana
```

2. ダッシュボードの問題：
- クエリの構文確認
- 時間範囲の適正化
- パネル設定の見直し

## 6. 発展的な使用法

### 6.1 カスタムメトリクス

1. ビジネスメトリクス：
```promql
# Todoの完了率
sum(todo_items_completed) / 
sum(todo_items_total) * 100
```

2. パフォーマンスメトリクス：
```promql
# DBクエリ時間
histogram_quantile(0.95, 
  rate(database_query_duration_seconds_bucket[5m]))
```

### 6.2 相関分析

```promql
# エラー率とレスポンスタイムの相関
rate(http_server_errors_total[5m])
/
rate(http_request_duration_seconds_count[5m])
```

次のステップでは、これらの監視設定を実際のアプリケーションに適用していきます。
