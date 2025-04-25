# OpenTelemetry Collectorの設定

## 概要

OpenTelemetry Collectorは、テレメトリデータの収集、処理、エクスポートを担う中心的なコンポーネントです。この章では、Collectorの設定ファイルの各セクションについて詳しく解説します。

## 基本構造

OpenTelemetry Collectorの設定ファイル（otel-collector-config.yaml）は、以下の主要セクションで構成されています：

```yaml
receivers:    # データの受信設定
  ...

processors:   # データの処理設定
  ...

exporters:    # データのエクスポート設定
  ...

service:      # パイプライン設定
  ...
```

## レシーバー（receivers）

レシーバーは、テレメトリデータを受信するためのエンドポイントを設定します。

```yaml
receivers:
  otlp:
    protocols:
      grpc:  # gRPCプロトコルの設定
        endpoint: "0.0.0.0:4317"
      http:  # HTTPプロトコルの設定
        endpoint: "0.0.0.0:4318"
```

### 主要な設定項目

1. OTLP/gRPC
   - デフォルトポート: 4317
   - 特徴: 高パフォーマンス、双方向ストリーミング
   - 用途: 本番環境での推奨プロトコル

2. OTLP/HTTP
   - デフォルトポート: 4318
   - 特徴: シンプル、ファイアウォールフレンドリー
   - 用途: 開発環境やテスト用

## プロセッサー（processors）

プロセッサーは、受信したデータの処理方法を定義します。

```yaml
processors:
  batch:
    timeout: 1s
    send_batch_size: 1024
```

### 主要な設定項目

1. バッチプロセッサー
   - timeout: バッチ処理のタイムアウト時間
   - send_batch_size: バッチサイズの上限
   - 目的: ネットワークとリソースの効率化

2. メモリーリミッター
```yaml
processors:
  memory_limiter:
    check_interval: 1s
    limit_mib: 1000
```

## エクスポーター（exporters）

エクスポーターは、処理したデータを各バックエンドに送信する方法を定義します。

```yaml
exporters:
  # Prometheusエクスポーター
  prometheus:
    endpoint: "0.0.0.0:8889"
  
  # Jaegerエクスポーター
  otlp:
    endpoint: "jaeger:4317"
    tls:
      insecure: true

  # ロギング（デバッグ用）
  logging:
    verbosity: detailed
```

### 主要な設定項目

1. Prometheusエクスポーター
   - endpoint: メトリクスの公開エンドポイント
   - namespace: メトリクス名のプレフィックス
   - const_labels: 固定のラベル

2. Jaegerエクスポーター
   - endpoint: Jaegerサーバーのアドレス
   - tls: セキュリティ設定
   - retry_on_failure: 再試行設定

## サービス設定（service）

サービスセクションでは、レシーバー、プロセッサー、エクスポーターを接続してパイプラインを定義します。

```yaml
service:
  pipelines:
    # トレースパイプライン
    traces:
      receivers: [otlp]
      processors: [batch]
      exporters: [otlp, logging]
    
    # メトリクスパイプライン
    metrics:
      receivers: [otlp]
      processors: [batch]
      exporters: [prometheus, logging]
```

### パイプラインの種類

1. トレースパイプライン
   - 分散トレーシングデータの処理
   - Jaegerへのエクスポート
   - デバッグ用ロギング

2. メトリクスパイプライン
   - メトリクスデータの処理
   - Prometheusへのエクスポート
   - デバッグ用ロギング

## 設定のベストプラクティス

1. パフォーマンス最適化
   ```yaml
   processors:
     batch:
       timeout: 10s
       send_batch_size: 10000
     memory_limiter:
       check_interval: 5s
       limit_mib: 2000
   ```

2. エラーハンドリング
   ```yaml
   exporters:
     otlp:
       retry_on_failure:
         enabled: true
         initial_interval: 5s
         max_interval: 30s
         max_elapsed_time: 300s
   ```

3. セキュリティ設定
   ```yaml
   receivers:
     otlp:
       protocols:
         grpc:
           tls:
             cert_file: /etc/certs/cert.pem
             key_file: /etc/certs/key.pem
   ```

## トラブルシューティング

### よくある問題と解決方法

1. データが受信されない
   - ポート番号の確認
   - TLS設定の確認
   - ネットワーク接続の確認

2. メモリ使用量が高い
   - バッチサイズの調整
   - メモリーリミッターの設定
   - 処理の最適化

3. エクスポートが失敗する
   - エンドポイントの確認
   - 認証情報の確認
   - リトライ設定の調整

## 設定の検証

### 設定ファイルの検証

```bash
# Collectorの設定チェック
docker compose exec otel-collector otelcol-contrib --config=/etc/otel-collector-config.yaml --check

# ログの確認
docker compose logs otel-collector
```

## 次のステップ

1. [トレース可視化の実装](03_trace_visualization.md)で、Jaegerを使用したトレースの可視化方法を学びます。

2. [メトリクス監視の実装](04_metrics_monitoring.md)で、PrometheusとGrafanaを使用したメトリクスの可視化方法を学びます。
