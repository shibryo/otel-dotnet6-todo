# 第3章：観測環境の構築 レビュー基準

## レビューの目的
- 観測環境が適切に構築されているか確認
- 各コンポーネントの設定が正しいか評価
- データの収集と可視化が機能しているか確認

## チェックリスト

### 1. OpenTelemetry Collectorの設定
- [ ] Collector設定ファイルの作成
  - receivers設定の確認
  - processors設定の確認
  - exporters設定の確認
  - service設定の確認
- [ ] Docker Compose設定
  - イメージの指定
  - ポートの設定
  - ボリュームマウント
  - 環境変数の設定
- [ ] パイプラインの設定
  - トレースパイプライン
  - メトリクスパイプライン
  - ログパイプライン

### 2. Jaegerの設定
- [ ] Jaeger設定
  - コンテナの設定
  - ポートの公開
  - UIアクセスの確認
- [ ] Collectorとの連携
  - エクスポーターの設定
  - データの受信確認
- [ ] トレース表示の確認
  - トレースの表示
  - スパンの詳細表示
  - タグとログの確認

### 3. Prometheusの設定
- [ ] Prometheus設定ファイル
  - スクレイプ設定
  - ジョブの定義
  - ターゲットの設定
- [ ] メトリクス収集の確認
  - エンドポイントの疎通確認
  - メトリクスの取得確認
  - ラベルの確認

### 4. Grafanaの設定
- [ ] 基本設定
  - データソースの追加
  - ダッシュボードの作成
  - アラートの設定
- [ ] ダッシュボード構成
  - アプリケーションメトリクス
  - システムメトリクス
  - エラーメトリクス

## コード品質チェック

### 1. Collector設定
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

exporters:
  jaeger:
    endpoint: jaeger:14250
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
      exporters: [jaeger]
    metrics:
      receivers: [otlp]
      processors: [batch]
      exporters: [prometheus]
```

### 2. Docker Compose設定
```yaml
# docker-compose.yml
version: '3.8'
services:
  otel-collector:
    image: otel/opentelemetry-collector:latest
    volumes:
      - ./otel-collector-config.yaml:/etc/otelcol/config.yaml
    ports:
      - "4317:4317"   # OTLP gRPC
      - "4318:4318"   # OTLP HTTP
      - "8889:8889"   # Prometheus exporter
    depends_on:
      - jaeger
      
  jaeger:
    image: jaegertracing/all-in-one:latest
    ports:
      - "16686:16686"  # UI
      - "14250:14250"  # gRPC
      
  prometheus:
    image: prom/prometheus:latest
    volumes:
      - ./prometheus.yml:/etc/prometheus/prometheus.yml
    ports:
      - "9090:9090"
      
  grafana:
    image: grafana/grafana:latest
    ports:
      - "3000:3000"
    environment:
      - GF_SECURITY_ADMIN_PASSWORD=admin
    depends_on:
      - prometheus
```

### 3. Prometheus設定
```yaml
# prometheus.yml
global:
  scrape_interval: 15s
  evaluation_interval: 15s

scrape_configs:
  - job_name: 'otel-collector'
    static_configs:
      - targets: ['otel-collector:8889']
```

## 理解度確認質問

### 1. OpenTelemetry Collectorの役割
```
Q: Collectorの主要な機能は何ですか？
A: 
- テレメトリデータの収集
- データの処理と変換
- 複数のバックエンドへのエクスポート
- バッファリングとバッチ処理
```

### 2. 可観測性スタックの連携
```
Q: 各コンポーネントの連携の流れを説明してください。
A:
1. アプリケーション → Collector (OTLP)
2. Collector → Jaeger (トレース)
3. Collector → Prometheus (メトリクス)
4. Prometheus → Grafana (可視化)
```

### 3. 監視設定
```
Q: 必要な監視項目は何ですか？
A:
- アプリケーションメトリクス（リクエスト数、レスポンスタイム等）
- システムメトリクス（CPU、メモリ使用率等）
- エラーメトリクス（エラー数、エラー率等）
- トレース情報（レイテンシ、エラーの原因等）
```

## よくあるエラーと解決策

### 1. Collectorへの接続エラー
```
考えられる原因：
1. ポートの競合
2. 設定ファイルの誤り
3. ネットワーク設定の問題

解決策：
1. ポート設定の確認と変更
2. 設定ファイルの構文チェック
3. ネットワーク接続の確認
```

### 2. メトリクスが表示されない
```
考えられる原因：
1. スクレイプ設定の誤り
2. エクスポーターの設定ミス
3. ラベルの不一致

解決策：
1. Prometheus設定の確認
2. エクスポーター設定の確認
3. メトリクス名とラベルの確認
```

## プログレスチェック

### 第3章の完了条件
1. OpenTelemetry Collector
- [ ] 設定ファイルの作成と確認
- [ ] Docker環境での起動確認
- [ ] テレメトリデータの受信確認

2. Jaeger
- [ ] UI接続の確認
- [ ] トレースデータの表示確認
- [ ] スパン詳細の確認

3. Prometheus
- [ ] メトリクス収集の確認
- [ ] ターゲットの状態確認
- [ ] クエリ実行の確認

4. Grafana
- [ ] データソース設定の確認
- [ ] ダッシュボードの作成
- [ ] メトリクス表示の確認

### 次のステップへの移行条件
1. すべてのチェックリストアイテムが完了
2. 各コンポーネントが正常に動作
3. データの収集と可視化が確認できる
4. 基本的なモニタリングが機能している
