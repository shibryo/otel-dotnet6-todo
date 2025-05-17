# 第3章：観測環境の構築 - 実装アドバイス

## 章の学習目標

1. 観測環境の理解
   - 各コンポーネントの役割と連携
   - データの流れと処理パイプライン
   - スケーラビリティとパフォーマンス

2. 各ツールの設定スキル
   - OpenTelemetry Collector
   - Jaeger
   - Prometheus
   - Grafana

3. 実践的な運用スキル
   - トラブルシューティング
   - パフォーマンスチューニング
   - セキュリティ考慮事項

## セッション別コンテンツ

### 1. Collector設定
参考資料：
- [Collector設定ガイド](https://opentelemetry.io/docs/collector/configuration/)
- [パイプライン設計](https://github.com/open-telemetry/opentelemetry-collector/blob/main/docs/design.md)

設定のポイント：
```yaml
# otel-collector-config.yaml
receivers:
  otlp:
    protocols:
      grpc:
        endpoint: "0.0.0.0:4317"
      http:
        endpoint: "0.0.0.0:4318"

processors:
  batch:
    timeout: 1s
    send_batch_size: 1024

exporters:
  jaeger:
    endpoint: "jaeger:14250"
    tls:
      insecure: true
  prometheus:
    endpoint: "0.0.0.0:8889"

service:
  pipelines:
    traces:
      receivers: [otlp]
      processors: [batch]
      exporters: [jaeger]
    metrics:
      receivers: [otlp]
      processors: [batch]
      exporters: [prometheus]
```

### 2. トレース可視化
参考資料：
- [Jaeger UI ガイド](https://www.jaegertracing.io/docs/latest/frontend-ui/)
- [トレース分析手法](https://opentelemetry.io/docs/concepts/signals/traces/)

実装例：
```yaml
# docker-compose.yml
services:
  jaeger:
    image: jaegertracing/all-in-one:latest
    ports:
      - "16686:16686"
      - "14250:14250"
    environment:
      - COLLECTOR_OTLP_ENABLED=true
```

### 3. メトリクス収集
参考資料：
- [Prometheus設定ガイド](https://prometheus.io/docs/prometheus/latest/configuration/configuration/)
- [PromQL基礎](https://prometheus.io/docs/prometheus/latest/querying/basics/)

設定例：
```yaml
# prometheus.yml
global:
  scrape_interval: 15s
  evaluation_interval: 15s

scrape_configs:
  - job_name: 'otel-collector'
    static_configs:
      - targets: ['otel-collector:8889']

  - job_name: 'todoapi'
    static_configs:
      - targets: ['todoapi:80']
```

クエリ例：
```promql
# リクエスト数の計測
rate(http_server_duration_count[5m])

# エラー率の計算
sum(rate(http_server_duration_count{status_code=~"5.."}[5m])) 
  / 
sum(rate(http_server_duration_count[5m]))
```

### 4. ダッシュボード作成
参考資料：
- [Grafanaダッシュボード](https://grafana.com/docs/grafana/latest/dashboards/)
- [可視化のベストプラクティス](https://grafana.com/docs/grafana/latest/best-practices/)

ダッシュボード例：
```json
{
  "panels": [
    {
      "title": "リクエスト数",
      "type": "graph",
      "datasource": "Prometheus",
      "targets": [
        {
          "expr": "rate(http_server_duration_count[5m])",
          "legendFormat": "{{operation}}"
        }
      ]
    },
    {
      "title": "レスポンスタイム",
      "type": "heatmap",
      "datasource": "Prometheus",
      "targets": [
        {
          "expr": "rate(http_server_duration_bucket[5m])",
          "format": "heatmap"
        }
      ]
    }
  ]
}
```

## トラブルシューティング

### 1. Collector接続エラー
```bash
# ログの確認
docker logs otel-collector

# 設定の検証
docker exec otel-collector otelcol-contrib --config-file=/etc/otelcol-contrib/config.yaml --dry-run
```

### 2. メトリクス収集の問題
```bash
# Prometheusターゲットの確認
curl http://localhost:9090/targets

# メトリクスの直接確認
curl http://localhost:8889/metrics
```

### 3. トレース欠落
```bash
# サンプリング設定の確認
# otel-collector-config.yaml
processors:
  probabilistic_sampler:
    sampling_percentage: 100

# Jaegerクエリの確認
curl http://localhost:16686/api/traces?service=todoapi
```

## セキュリティ考慮事項

### 1. アクセス制御
```yaml
# 認証設定例
receivers:
  otlp:
    protocols:
      grpc:
        endpoint: "0.0.0.0:4317"
        tls:
          cert_file: "/certs/server.crt"
          key_file: "/certs/server.key"
```

### 2. データ保護
- 機密情報のフィルタリング
- TLS通信の確保
- アクセスログの監視

## パフォーマンス最適化

### 1. Collector設定
```yaml
processors:
  batch:
    timeout: 1s
    send_batch_size: 1024
  memory_limiter:
    check_interval: 1s
    limit_mib: 1024
```

### 2. Prometheusチューニング
```yaml
global:
  scrape_interval: 15s
  scrape_timeout: 10s

scrape_configs:
  - job_name: 'otel-collector'
    scrape_interval: 5s
    scrape_timeout: 4s
```

### 3. Grafanaリソース管理
```ini
[server]
max_connections = 100
idle_timeout = 180

[dashboards]
versions_to_keep = 20
```

## デバッグのコツ

### 1. Collectorデバッグ
```bash
# デバッグモードでの起動
docker run -e COLLECTOR_DEBUG=true otel/opentelemetry-collector-contrib

# メトリクスエンドポイントの確認
curl http://localhost:8889/metrics
```

### 2. Prometheus確認
```bash
# クエリ実行時間の確認
curl -g 'http://localhost:9090/api/v1/query_range?query=rate(http_server_duration_count[5m])&start=2024-01-01T20:10:30.781Z&end=2024-01-01T20:11:00.781Z&step=15s'
```

### 3. Jaegerトラブルシューティング
```bash
# スパン数の確認
curl http://localhost:16686/api/traces?service=todoapi&operation=CreateTodoItem

# 特定のトレースの詳細
curl http://localhost:16686/api/traces/{trace-id}
