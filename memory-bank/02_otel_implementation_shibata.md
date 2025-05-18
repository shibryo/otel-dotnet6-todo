# OpenTelemetry実装メモ - 柴田

## 第2週目（5/18）

### OpenTelemetryの実装と監視環境の構築

#### 1. パッケージ導入
- TodoApiプロジェクトに必要なパッケージを追加
  - OpenTelemetry.Extensions.Hosting
  - OpenTelemetry.Instrumentation.AspNetCore
  - OpenTelemetry.Instrumentation.Http
  - OpenTelemetry.Instrumentation.EntityFrameworkCore
  - OpenTelemetry.Exporter.OpenTelemetryProtocol

#### 2. Program.csでの実装
```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
        tracerProviderBuilder
            .AddSource("TodoApi")
            .SetResourceBuilder(
                ResourceBuilder.CreateDefault()
                    .AddService(serviceName: "TodoApi"))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation()
            .AddOtlpExporter(opts => {
                opts.Endpoint = new Uri("http://otel-collector:4317");
                opts.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
            }))
```

#### 3. Collector設定
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

exporters:
  otlp/jaeger:
    endpoint: jaeger:4317
    tls:
      insecure: true
  prometheus:
    endpoint: 0.0.0.0:8889

service:
  pipelines:
    traces:
      receivers: [otlp]
      processors: [batch]
      exporters: [otlp/jaeger]
    metrics:
      receivers: [otlp]
      processors: [batch]
      exporters: [prometheus]
```

### 課題と次のステップ

#### 現在の課題
1. トレースデータの転送問題
   - TodoApiからCollectorへの接続は確立
   - CollectorからJaegerへのトレース転送が機能していない
   - JaegerとCollectorの接続設定に問題の可能性

#### 次回の対応方針
1. 設定の見直し
   - JaegerのgRPCポート設定の確認
   - Collector-Jaeger間の接続方式の再検討
   - トレース送信の検証方法の確立

2. 検証ステップ
   - ログ出力の詳細化
   - トレース送信時のデバッグ情報の確認
   - 各コンポーネント間の接続テスト
