# 第3章：観測環境の構築 実装アドバイス

## OpenTelemetry Collector設定

### 1. 基本設定
```yaml
# otel-collector-config.yaml
receivers:
  otlp:
    protocols:
      grpc:
        endpoint: 0.0.0.0:4317
      http:
        endpoint: 0.0.0.0:4318

processors:
  batch:
    timeout: 1s
    send_batch_size: 1024
  memory_limiter:
    check_interval: 1s
    limit_mib: 1000

exporters:
  jaeger:
    endpoint: jaeger:14250
    tls:
      insecure: true
  prometheus:
    endpoint: 0.0.0.0:8889
    namespace: todo_app
    const_labels:
      env: development

service:
  telemetry:
    logs:
      level: "debug"  # 開発時は"debug"、本番は"info"
  pipelines:
    traces:
      receivers: [otlp]
      processors: [memory_limiter, batch]
      exporters: [jaeger]
    metrics:
      receivers: [otlp]
      processors: [memory_limiter, batch]
      exporters: [prometheus]
```

### 2. Docker環境設定
```yaml
# docker-compose.yml
version: '3.8'
services:
  otel-collector:
    image: otel/opentelemetry-collector:latest
    container_name: otel-collector
    volumes:
      - ./otel-collector-config.yaml:/etc/otelcol/config.yaml
    ports:
      - "4317:4317"   # OTLP gRPC
      - "4318:4318"   # OTLP HTTP
      - "8889:8889"   # Prometheus metrics
    restart: unless-stopped
    networks:
      - monitoring
    depends_on:
      - jaeger
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:13133"]
      interval: 10s
      timeout: 5s
      retries: 5

networks:
  monitoring:
    name: monitoring
```

## Jaeger設定

### 1. Jaegerの設定
```yaml
# docker-compose.yml (Jaeger部分)
jaeger:
  image: jaegertracing/all-in-one:latest
  container_name: jaeger
  ports:
    - "16686:16686"  # UI
    - "14250:14250"  # gRPC collector
    - "14268:14268"  # HTTP collector
    - "6831:6831/udp"  # Thrift compact
  environment:
    - COLLECTOR_OTLP_ENABLED=true
    - SPAN_STORAGE_TYPE=memory  # 開発環境用
  networks:
    - monitoring
  healthcheck:
    test: ["CMD", "wget", "--spider", "http://localhost:16686"]
    interval: 10s
    timeout: 5s
    retries: 5
```

### 2. トレース検索のポイント
```javascript
// トレース検索クエリの例
{
  "service": "todo-api",
  "operation": "CreateTodoItem",
  "tags": {
    "error": "true"
  },
  "minDuration": "100ms"
}
```

## Prometheus設定

### 1. 基本設定
```yaml
# prometheus.yml
global:
  scrape_interval: 15s
  evaluation_interval: 15s

scrape_configs:
  - job_name: 'otel-collector'
    static_configs:
      - targets: ['otel-collector:8889']
    metric_relabel_configs:
      - source_labels: [namespace]
        regex: todo_app
        action: keep

  - job_name: 'todo-api'
    static_configs:
      - targets: ['todo-api:80']
    metrics_path: '/metrics'
```

### 2. Docker設定
```yaml
# docker-compose.yml (Prometheus部分)
prometheus:
  image: prom/prometheus:latest
  container_name: prometheus
  volumes:
    - ./prometheus.yml:/etc/prometheus/prometheus.yml
    - prometheus_data:/prometheus
  ports:
    - "9090:9090"
  command:
    - '--config.file=/etc/prometheus/prometheus.yml'
    - '--storage.tsdb.path=/prometheus'
    - '--storage.tsdb.retention.time=15d'
  networks:
    - monitoring
  healthcheck:
    test: ["CMD", "wget", "--spider", "http://localhost:9090/-/healthy"]
    interval: 10s
    timeout: 5s
    retries: 5

volumes:
  prometheus_data:
```

## Grafana設定

### 1. 基本設定
```yaml
# docker-compose.yml (Grafana部分)
grafana:
  image: grafana/grafana:latest
  container_name: grafana
  ports:
    - "3000:3000"
  environment:
    - GF_SECURITY_ADMIN_USER=admin
    - GF_SECURITY_ADMIN_PASSWORD=admin
    - GF_USERS_ALLOW_SIGN_UP=false
  volumes:
    - grafana_data:/var/lib/grafana
    - ./grafana/provisioning:/etc/grafana/provisioning
  networks:
    - monitoring
  depends_on:
    - prometheus
  healthcheck:
    test: ["CMD", "wget", "--spider", "http://localhost:3000/api/health"]
    interval: 10s
    timeout: 5s
    retries: 5

volumes:
  grafana_data:
```

### 2. データソース設定
```yaml
# grafana/provisioning/datasources/datasource.yml
apiVersion: 1

datasources:
  - name: Prometheus
    type: prometheus
    access: proxy
    url: http://prometheus:9090
    isDefault: true
    editable: false
```

### 3. ダッシュボード設定
```yaml
# grafana/provisioning/dashboards/dashboard.yml
apiVersion: 1

providers:
  - name: 'Default'
    orgId: 1
    folder: ''
    type: file
    disableDeletion: false
    editable: true
    options:
      path: /etc/grafana/provisioning/dashboards
```

### 4. サンプルダッシュボード
```json
{
  "dashboard": {
    "id": null,
    "title": "Todo Application Metrics",
    "panels": [
      {
        "title": "Request Rate",
        "type": "graph",
        "datasource": "Prometheus",
        "targets": [
          {
            "expr": "rate(http_server_requests_seconds_count{job=\"todo-api\"}[5m])",
            "legendFormat": "{{method}} {{status}}"
          }
        ]
      },
      {
        "title": "Response Time",
        "type": "graph",
        "datasource": "Prometheus",
        "targets": [
          {
            "expr": "rate(http_server_requests_seconds_sum{job=\"todo-api\"}[5m]) / rate(http_server_requests_seconds_count{job=\"todo-api\"}[5m])",
            "legendFormat": "{{method}} {{status}}"
          }
        ]
      }
    ]
  }
}
```

## 監視設定のベストプラクティス

### 1. メトリクス命名規則
```plaintext
# 命名規則
{namespace}_{subsystem}_{metric_name}_{unit}

例：
todo_http_requests_total
todo_db_operation_duration_seconds
todo_items_created_total
```

### 2. ラベル設定のベストプラクティス
```plaintext
# 共通ラベル
env: 環境（development, staging, production）
service: サービス名（todo-api, todo-web）
instance: インスタンス識別子

# メトリクス固有のラベル
method: HTTPメソッド
status: HTTPステータスコード
operation: 操作種別
```

### 3. アラート設定
```yaml
# prometheus/alerts.yml
groups:
  - name: todo_app_alerts
    rules:
      - alert: HighErrorRate
        expr: |
          rate(http_server_requests_seconds_count{status=~"5.."}[5m])
          /
          rate(http_server_requests_seconds_count[5m])
          > 0.1
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: High HTTP error rate
          description: Error rate is above 10% for 5 minutes

      - alert: SlowResponses
        expr: |
          rate(http_server_requests_seconds_sum[5m])
          /
          rate(http_server_requests_seconds_count[5m])
          > 0.5
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: Slow response times
          description: Average response time is above 500ms for 5 minutes
```

## デバッグとトラブルシューティング

### 1. OpenTelemetry Collector
```bash
# ログの確認
docker logs otel-collector

# 設定の検証
docker exec otel-collector otelcol-contrib --config=/etc/otelcol/config.yaml --dry-run

# メトリクスエンドポイントの確認
curl http://localhost:8889/metrics
```

### 2. Jaeger
```bash
# UIアクセス
open http://localhost:16686

# トレース検索のヒント
- サービス名で絞り込み
- 時間範囲を適切に設定
- エラーを含むトレースをフィルタ
```

### 3. Prometheus
```bash
# ターゲットの状態確認
open http://localhost:9090/targets

# クエリの例
# リクエスト数
rate(http_server_requests_seconds_count{job="todo-api"}[5m])

# エラー率
sum(rate(http_server_requests_seconds_count{job="todo-api",status=~"5.."}[5m]))
/
sum(rate(http_server_requests_seconds_count{job="todo-api"}[5m]))
```

### 4. Grafana
```bash
# データソースの確認
curl http://localhost:3000/api/datasources

# ダッシュボードの確認
curl http://localhost:3000/api/dashboards
```

## セキュリティ考慮事項

### 1. ネットワークセキュリティ
```yaml
# docker-compose.yml
networks:
  monitoring:
    internal: true  # 外部からの直接アクセスを防止
```

### 2. 認証設定
```yaml
# Grafana認証
GF_SECURITY_ADMIN_PASSWORD: "${GRAFANA_ADMIN_PASSWORD}"
GF_USERS_ALLOW_SIGN_UP: "false"

# Prometheus認証（基本認証）
basic_auth_users:
  admin: "${PROMETHEUS_PASSWORD}"
```

### 3. TLS設定
```yaml
# otel-collector-config.yaml
exporters:
  jaeger:
    endpoint: jaeger:14250
    tls:
      cert_file: /etc/otel/cert.pem
      key_file: /etc/otel/key.pem
