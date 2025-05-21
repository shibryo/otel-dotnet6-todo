# OpenTelemetry Collectorの設定

実際にCollectorの設定を行いながら、その役割と機能について学んでいきましょう。

## 1. Collector設定ファイルの作成

以下の内容で`otel-collector-config.yaml`を作成します：

```yaml
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
    send_batch_max_size: 2048

exporters:
  debug:
    verbosity: detailed
  otlp/jaeger:
    endpoint: jaeger:4317
    tls:
      insecure: true
  prometheus:
    endpoint: 0.0.0.0:8889
    namespace: todo_app

service:
  pipelines:
    traces:
      receivers: [otlp]
      processors: [batch]
      exporters: [otlp/jaeger, debug]
    metrics:
      receivers: [otlp]
      processors: [batch]
      exporters: [prometheus, debug]
    logs:
      receivers: [otlp]
      processors: [batch]
      exporters: [debug]
```

> 💡 設定ファイルの構造
> - receivers: データの受信方法を定義
> - processors: 受信したデータの処理方法を定義
> - exporters: 処理したデータの転送先を定義
> - service: データの流れ（パイプライン）を定義

## 2. 設定の適用と確認

1. Collectorの再起動：
```bash
docker compose restart otelcol
```

2. ログの確認：
```bash
docker compose logs -f otelcol
```

> 💡 なぜバッチ処理が必要か？
> - ネットワーク通信の削減
> - バックエンドへの負荷軽減
> - メモリ使用量の最適化

## 3. レシーバーの設定

### 3.1 動作確認

```bash
# gRPCエンドポイントの確認
docker compose exec otelcol nc -zv 0.0.0.0 4317

# HTTPエンドポイントの確認
docker compose exec otelcol nc -zv 0.0.0.0 4318
```

エラーが出る場合は以下を確認：
- ポートの重複
- ファイアウォールの設定
- コンテナのネットワーク設定

### 3.2 設定のカスタマイズ

TLS設定を追加する例：
```yaml
receivers:
  otlp:
    protocols:
      grpc:
        endpoint: 0.0.0.0:4317
        tls:
          cert_file: /etc/certs/server.crt
          key_file: /etc/certs/server.key
```

## 4. プロセッサーの設定

### 4.1 バッチ設定の調整

低レイテンシー重視の場合：
```yaml
processors:
  batch:
    timeout: 100ms
    send_batch_size: 100
```

スループット重視の場合：
```yaml
processors:
  batch:
    timeout: 5s
    send_batch_size: 10000
```

### 4.2 メモリ使用量の監視

```bash
# メモリ使用量の確認
docker stats otelcol

# メトリクスの確認
curl http://localhost:8889/metrics | grep otelcol_process_memory
```

## 5. エクスポーターの設定

### 5.1 Jaegerエクスポーターの確認

```bash
# Jaeger接続の確認
docker compose exec otelcol nc -zv jaeger 4317

# デバッグログの確認
docker compose logs -f jaeger
```

### 5.2 Prometheusエクスポーターの確認

```bash
# メトリクスエンドポイントの確認
curl http://localhost:8889/metrics

# Prometheusのターゲット確認
curl http://localhost:9090/api/v1/targets
```

## 6. 高度な設定

### 6.1 カスタム属性の追加

```yaml
processors:
  attributes:
    actions:
      - key: environment
        value: development
        action: insert
```

### 6.2 フィルターの設定

```yaml
processors:
  filter:
    metrics:
      include:
        match_type: regexp
        metric_names:
          - .*todo.*
```

## 7. トラブルシューティング

### 7.1 設定の検証

```bash
# 設定ファイルの構文チェック
docker compose exec otelcol otelcol --config=/etc/otelcol/config.yaml --validate-config
```

### 7.2 よくあるエラー対処

1. データが受信されない：
```bash
# ポートの確認
docker compose exec otelcol netstat -tulpn

# ログレベルの変更
service:
  telemetry:
    logs:
      level: debug
```

2. メモリ使用量が高い：
- バッチサイズの調整
- 送信間隔の調整
- 不要なプロセッサーの削除

次のセクションでは、Jaegerを使用したトレース可視化を行います。
