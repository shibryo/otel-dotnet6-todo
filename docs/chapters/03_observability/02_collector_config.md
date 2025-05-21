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

1. Tiltによる設定の適用：
```bash
# 設定変更を監視して自動反映
tilt up

# ログの確認（Tilt UI）
http://localhost:10350
```

> 💡 Tiltのホットリロード
> - 設定ファイルの変更を検知して自動で再起動
> - ログをリアルタイムで確認可能
> - 開発サイクルの効率化

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

## 7. トラブルシューティングガイド

### 7.1 問題の切り分け方

1. 症状の確認
- [ ] データが受信されない
- [ ] エクスポートが失敗する
- [ ] メモリ使用量が異常
- [ ] パフォーマンスが低下

2. ログの確認
```bash
# Collectorのログ確認
docker compose logs -f otelcol

# デバッグレベルでの実行
docker compose exec otelcol otelcol --config=/etc/otelcol/config.yaml --log-level=debug

# エラーの確認
docker compose logs | grep -i error
docker compose logs | grep -i failed
```

3. 設定の確認
```bash
# 設定ファイルの構文チェック
docker compose exec otelcol otelcol --config=/etc/otelcol/config.yaml --validate-config

# 受信ポートの確認
docker compose exec otelcol netstat -tulpn

# メトリクスの確認
curl http://localhost:8889/metrics
```

### 7.2 よくある問題と解決策

1. データ受信の問題
- 原因：
  * ポートの設定ミス
  * プロトコルの不一致
  * ネットワーク接続の問題
- 解決策：
  * エンドポイント設定の確認
  * プロトコル設定の確認
  * ネットワーク疎通の確認

2. メモリ使用量の問題
- 原因：
  * バッチサイズが大きすぎる
  * データ量が多すぎる
  * メモリリーク
- 解決策：
  * バッチ設定の最適化
  * サンプリングの導入
  * 不要なプロセッサーの削除

3. エクスポートの問題
- 原因：
  * バックエンドの接続エラー
  * 設定の誤り
  * 認証の問題
- 解決策：
  * バックエンド接続の確認
  * エクスポーター設定の見直し
  * TLS/認証設定の確認

### 7.3 診断コマンド集

1. 状態確認コマンド
```bash
# プロセス状態の確認
docker compose ps otelcol

# メモリ使用量の確認
docker stats otelcol

# 設定の確認
docker compose exec otelcol cat /etc/otelcol/config.yaml
```

2. ログ確認コマンド
```bash
# 詳細ログの有効化
docker compose exec otelcol otelcol --config=/etc/otelcol/config.yaml --log-level=debug

# エラーログの確認
docker compose logs otelcol | grep -i error

# 特定のコンポーネントのログ
docker compose logs otelcol | grep -i "processor::batch"
```

3. 接続確認コマンド
```bash
# Jaeger接続の確認
docker compose exec otelcol nc -zv jaeger 4317

# Prometheusエンドポイントの確認
curl http://localhost:8889/metrics

# ネットワーク状態の確認
docker network inspect $(docker compose ps -q)
```

> 💡 効率的なトラブルシューティングのポイント
> - 設定ファイルの変更前にバックアップを作成
> - 一度に1つの設定のみ変更
> - デバッグログを活用して問題を特定
> - パイプラインの各段階で動作を確認

次のセクションでは、Jaegerを使用したトレース可視化を行います。
