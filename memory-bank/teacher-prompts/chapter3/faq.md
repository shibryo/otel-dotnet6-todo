# 第3章：観測環境の構築 よくある質問と回答

## 観測環境の基本概念

### Q1: 可観測性スタックの各コンポーネントの役割は何ですか？
A: 各コンポーネントには以下のような役割があります：

1. OpenTelemetry Collector
   - テレメトリデータの収集
   - データの加工・変換
   - 複数のバックエンドへの転送
   - バッファリングとバッチ処理

2. Jaeger
   - 分散トレーシングの可視化
   - トレースの検索・分析
   - スパン詳細の表示
   - 依存関係の可視化

3. Prometheus
   - メトリクスデータの収集
   - 時系列データの保存
   - クエリ機能の提供
   - アラートの設定

4. Grafana
   - データの可視化
   - ダッシュボードの作成
   - 複数データソースの統合
   - アラート通知の管理

### Q2: コンテナ間の通信はどのように行われますか？
A: コンテナ間通信の基本：

1. ネットワーク設定
```yaml
# docker-compose.yml
networks:
  monitoring:
    name: monitoring
```

2. サービス間の参照
```yaml
services:
  otel-collector:
    networks:
      - monitoring
    depends_on:
      - jaeger
```

3. 通信先の指定
```yaml
exporters:
  jaeger:
    endpoint: jaeger:14250  # サービス名で参照
```

## 環境構築に関する質問

### Q3: Docker Composeファイルの書き方がわかりません
A: 基本的な構造と書き方：

1. サービス定義
```yaml
version: '3.8'
services:
  service-name:
    image: image-name
    ports:
      - "host-port:container-port"
    volumes:
      - ./local-path:/container-path
    environment:
      - KEY=value
```

2. よくある設定
```yaml
# ヘルスチェック
healthcheck:
  test: ["CMD", "curl", "-f", "http://localhost/health"]
  interval: 10s
  timeout: 5s
  retries: 3

# 依存関係
depends_on:
  - other-service

# 再起動ポリシー
restart: unless-stopped
```

### Q4: 各コンポーネントのポート設定は何を使えばよいですか？
A: 一般的なポート設定：

```yaml
# 標準的なポート番号
otel-collector:
  - 4317: OTLP gRPC
  - 4318: OTLP HTTP
  - 8889: Prometheus metrics

jaeger:
  - 16686: UI
  - 14250: gRPC collector
  - 14268: HTTP collector
  - 6831: UDP compact

prometheus:
  - 9090: Web UI & API

grafana:
  - 3000: Web UI
```

## トラブルシューティング

### Q5: コンテナが起動しません
A: 以下の手順で確認してください：

1. ログの確認
```bash
# コンテナのログ確認
docker-compose logs [service-name]

# リアルタイムログ
docker-compose logs -f [service-name]
```

2. 設定ファイルの確認
```bash
# 設定の検証
docker-compose config

# 特定のサービスの設定
docker-compose config [service-name]
```

3. ネットワークの確認
```bash
# ネットワーク一覧
docker network ls

# ネットワーク詳細
docker network inspect monitoring
```

### Q6: メトリクスが収集されません
A: 以下の点を確認してください：

1. Collector設定
```yaml
# otel-collector-config.yaml
receivers:
  otlp:
    protocols:
      grpc:
        endpoint: 0.0.0.0:4317  # すべてのインターフェースでリッスン

exporters:
  prometheus:
    endpoint: 0.0.0.0:8889  # すべてのインターフェースで公開
```

2. Prometheus設定
```yaml
# prometheus.yml
scrape_configs:
  - job_name: 'otel-collector'
    static_configs:
      - targets: ['otel-collector:8889']
```

### Q7: Grafanaでデータが表示されません
A: 以下の手順で確認してください：

1. データソースの確認
```yaml
# datasource.yml
datasources:
  - name: Prometheus
    type: prometheus
    access: proxy
    url: http://prometheus:9090  # コンテナ名での参照
    isDefault: true
```

2. クエリの確認
```yaml
# メトリクスの存在確認
expr: up{job="todo-api"}

# データの有無確認
expr: count(http_server_requests_seconds_count)
```

## パフォーマンスとスケーリング

### Q8: Collectorのパフォーマンスを最適化するには？
A: 以下の設定を調整してください：

1. バッチ処理の設定
```yaml
processors:
  batch:
    timeout: 1s
    send_batch_size: 1024
```

2. メモリ制限の設定
```yaml
processors:
  memory_limiter:
    check_interval: 1s
    limit_mib: 1000
```

### Q9: Prometheusのストレージ管理は？
A: 以下の設定で管理します：

1. 保存期間の設定
```yaml
command:
  - '--storage.tsdb.retention.time=15d'
  - '--storage.tsdb.retention.size=10GB'
```

2. コンパクションの設定
```yaml
command:
  - '--storage.tsdb.min-block-duration=2h'
  - '--storage.tsdb.max-block-duration=2h'
```

## セキュリティ

### Q10: 認証の設定方法は？
A: 各コンポーネントの認証設定：

1. Grafana
```yaml
environment:
  - GF_SECURITY_ADMIN_USER=${GRAFANA_USER}
  - GF_SECURITY_ADMIN_PASSWORD=${GRAFANA_PASSWORD}
  - GF_USERS_ALLOW_SIGN_UP=false
```

2. Prometheus
```yaml
basic_auth_users:
  admin: ${PROMETHEUS_PASSWORD}
```

### Q11: 通信の暗号化は必要ですか？
A: 環境に応じて以下を検討：

1. 開発環境
```yaml
# 通常は暗号化不要
networks:
  monitoring:
    internal: true  # 外部アクセス制限
```

2. 本番環境
```yaml
# TLS設定
tls:
  cert_file: /path/to/cert
  key_file: /path/to/key
  ca_file: /path/to/ca
```

### Q12: データのバックアップは？
A: 各コンポーネントのバックアップ方法：

1. ボリュームのバックアップ
```bash
# ボリュームの一覧
docker volume ls

# バックアップの作成
docker run --rm -v prometheus_data:/source -v $(pwd):/backup \
  alpine tar czf /backup/prometheus-backup.tar.gz /source
```

2. 設定ファイルの管理
```bash
# バージョン管理で設定を保存
git add docker-compose.yml prometheus.yml
git commit -m "Update monitoring configuration"
