# 第3章：観測環境の構築

## 目的

この章では、OpenTelemetry Collectorを設定し、トレースの可視化とメトリクスの監視環境を構築します。

## 前提条件

- 第2章の実装が完了していること
- Docker Composeの基本的な理解
- 監視の基本概念の理解
  - トレース可視化
  - メトリクス監視
  - ダッシュボード

## 実装ステップ

1. OpenTelemetry Collectorの設定
   ```yaml
   # docker/otel-collector-config.yaml
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

2. Prometheusの設定
   ```yaml
   # docker/prometheus.yml
   global:
     scrape_interval: 15s
     evaluation_interval: 15s

   scrape_configs:
     - job_name: 'otel-collector'
       static_configs:
         - targets: ['otel-collector:8889']
       metric_relabel_configs:
         - source_labels: [__name__]
           regex: '.*todo.*'
           action: keep
   ```

3. docker-compose.ymlの更新
   ```yaml
   services:
     # 既存のサービスに加えて以下を追加
     otel-collector:
       image: otel/opentelemetry-collector:0.88.0
       command: ["--config=/etc/otel-collector-config.yaml"]
       volumes:
         - ./otel-collector-config.yaml:/etc/otel-collector-config.yaml
       ports:
         - "4317:4317"   # OTLP gRPC receiver
         - "4318:4318"   # OTLP http receiver
         - "8889:8889"   # Prometheus exporter
       depends_on:
         - jaeger

     jaeger:
       image: jaegertracing/all-in-one:1.49
       ports:
         - "16686:16686"  # UI
         - "14250:14250"  # Model
       environment:
         - COLLECTOR_OTLP_ENABLED=true
         - COLLECTOR_OTLP_GRPC_PORT=4317

     prometheus:
       image: prom/prometheus:v2.45.0
       volumes:
         - ./prometheus.yml:/etc/prometheus/prometheus.yml
         - prometheus-data:/prometheus
       ports:
         - "9090:9090"
       depends_on:
         - otel-collector

     grafana:
       image: grafana/grafana:10.0.3
       volumes:
         - grafana-data:/var/lib/grafana
       environment:
         - GF_SECURITY_ADMIN_PASSWORD=admin
         - GF_SECURITY_ADMIN_USER=admin
       ports:
         - "3001:3000"
       depends_on:
         - prometheus

   volumes:
     prometheus-data:
     grafana-data:
   ```

4. Program.csの更新（OTLPエクスポーターの追加）
   ```csharp
   // OpenTelemetry設定の更新
   builder.Services.AddOpenTelemetry()
       .WithTracing(tracerProviderBuilder =>
       {
           tracerProviderBuilder
               .AddSource("TodoApi")
               .SetResourceBuilder(
                   ResourceBuilder.CreateDefault()
                       .AddService(serviceName: "TodoApi", serviceVersion: "1.0.0"))
               .AddAspNetCoreInstrumentation()
               .AddHttpClientInstrumentation()
               .AddEntityFrameworkCoreInstrumentation()
               .AddConsoleExporter()
               .AddOtlpExporter(opts => {
                   opts.Endpoint = new Uri("http://otel-collector:4317");
               });
       })
       .WithMetrics(metricsProviderBuilder =>
       {
           metricsProviderBuilder
               .SetResourceBuilder(
                   ResourceBuilder.CreateDefault()
                       .AddService(serviceName: "TodoApi", serviceVersion: "1.0.0"))
               .AddAspNetCoreInstrumentation()
               .AddHttpClientInstrumentation()
               .AddConsoleExporter()
               .AddOtlpExporter(opts => {
                   opts.Endpoint = new Uri("http://otel-collector:4317");
               });
       });
   ```

## 動作確認手順

1. 環境の起動
   ```bash
   cd src/start
   docker-compose down
   docker-compose up -d --build
   ```

2. 各コンポーネントの確認
   - Jaeger UI: http://localhost:16686
   - Prometheus: http://localhost:9090
   - Grafana: http://localhost:3001 (admin/admin)

3. テストデータの生成
   ```bash
   # 複数のTodo項目を作成
   for i in {1..5}; do
     curl -X POST http://localhost:5000/api/TodoItems \
          -H "Content-Type: application/json" \
          -d "{\"title\":\"Todo $i\",\"isComplete\":false}"
   done

   # いくつかのTodo項目を完了に設定
   for i in {1..3}; do
     curl -X PUT http://localhost:5000/api/TodoItems/$i \
          -H "Content-Type: application/json" \
          -d "{\"id\":$i,\"title\":\"Todo $i\",\"isComplete\":true}"
   done
   ```

## Grafanaダッシュボードの設定

1. データソースの追加
   - Prometheusを追加（URL: http://prometheus:9090）
   - 接続テストで成功を確認

2. ダッシュボードの作成
   - 新規ダッシュボードを作成
   - 以下のパネルを追加：
     - Todo作成数（Counter）
     - アクティブなTodo数（Gauge）
     - Todo完了までの時間（Histogram）
     - APIレスポンスタイム（Time series）

3. クエリ例
   ```promql
   # Todo作成数
   rate(todo_created_total[5m])

   # アクティブなTodo数
   todo_active

   # Todo完了までの時間（95パーセンタイル）
   histogram_quantile(0.95, rate(todo_completion_time_bucket[5m]))

   # APIレスポンスタイム
   rate(http_server_duration_seconds_sum[5m]) / 
   rate(http_server_duration_seconds_count[5m])
   ```

## トラブルシューティング

1. Collectorの問題
   - ログの確認: `docker-compose logs otel-collector`
   - 設定ファイルの構文確認
   - ポートの開放状態確認

2. Jaegerの問題
   - UIアクセス確認
   - Collectorからの接続確認
   - トレースデータの到着確認

3. Prometheusの問題
   - ターゲットの状態確認
   - メトリクスの取得確認
   - スクレイピング設定の確認

4. Grafanaの問題
   - データソース接続確認
   - クエリ結果の確認
   - パーミッションの確認

## 次のステップ

監視環境の基本設定が完了したら、次章で以下の高度な機能を実装します：
- サンプリング設定の最適化
- カスタムメトリクスの追加
- アラート設定
- パフォーマンス分析
