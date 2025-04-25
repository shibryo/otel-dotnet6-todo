# OpenTelemetry SDKのインストールと設定

## 概要

この章では、TodoアプリケーションにOpenTelemetry SDKをインストールし、基本的な設定を行います。実際のコードを見ながら、各設定の意味と目的を理解していきましょう。

## 必要なパッケージのインストール

### NuGetパッケージの追加

```xml
<ItemGroup>
    <!-- OpenTelemetry基本パッケージ -->
    <PackageReference Include="OpenTelemetry" Version="1.5.0" />
    <PackageReference Include="OpenTelemetry.Api" Version="1.5.0" />
    
    <!-- 各種エクスポーター -->
    <PackageReference Include="OpenTelemetry.Exporter.Console" Version="1.5.0" />
    <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.5.0" />
    
    <!-- インストルメンテーション -->
    <PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.5.0-beta.1" />
    <PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.5.0-beta.1" />
    <PackageReference Include="OpenTelemetry.Instrumentation.EntityFrameworkCore" Version="1.0.0-beta.7" />
    
    <!-- ホスティング関連 -->
    <PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.5.0" />
    <PackageReference Include="OpenTelemetry.Extensions.DependencyInjection" Version="1.4.0-rc.2" />
</ItemGroup>
```

### パッケージの役割説明

1. 基本パッケージ
   - `OpenTelemetry`: SDKの中核機能
   - `OpenTelemetry.Api`: API定義

2. エクスポーター
   - `OpenTelemetry.Exporter.Console`: デバッグ用コンソール出力
   - `OpenTelemetry.Exporter.OpenTelemetryProtocol`: OTLPエクスポーター

3. 自動計装
   - `OpenTelemetry.Instrumentation.AspNetCore`: ASP.NET Core用
   - `OpenTelemetry.Instrumentation.Http`: HTTPクライアント用
   - `OpenTelemetry.Instrumentation.EntityFrameworkCore`: EF Core用

4. 統合サポート
   - `OpenTelemetry.Extensions.Hosting`: ホスティング統合
   - `OpenTelemetry.Extensions.DependencyInjection`: DI統合

## SDKの初期化設定

### Program.csでの設定

```csharp
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;

var builder = WebApplication.CreateBuilder(args);

// OpenTelemetryの設定
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            .AddSource("TodoApi")  // アプリケーション用のソース
            .SetResourceBuilder(   // リソース情報の設定
                ResourceBuilder.CreateDefault()
                    .AddService(serviceName: "TodoApi", serviceVersion: "1.0.0"))
            .AddAspNetCoreInstrumentation()    // ASP.NET Coreの自動計装
            .AddHttpClientInstrumentation()     // HTTPクライアントの自動計装
            .AddEntityFrameworkCoreInstrumentation()  // EF Coreの自動計装
            .AddConsoleExporter()              // デバッグ用コンソール出力
            .AddOtlpExporter(opts => {         // OTLPエクスポーター設定
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

### 設定の解説

1. リソース設定
   ```csharp
   .SetResourceBuilder(
       ResourceBuilder.CreateDefault()
           .AddService(serviceName: "TodoApi", serviceVersion: "1.0.0"))
   ```
   - サービス名とバージョンを設定
   - テレメトリーデータの送信元を識別

2. トレース設定
   ```csharp
   .AddSource("TodoApi")
   .AddAspNetCoreInstrumentation()
   .AddHttpClientInstrumentation()
   .AddEntityFrameworkCoreInstrumentation()
   ```
   - アプリケーション用のソースを登録
   - 各種フレームワークの自動計装を有効化

3. エクスポーター設定
   ```csharp
   .AddConsoleExporter()
   .AddOtlpExporter(opts => {
       opts.Endpoint = new Uri("http://otel-collector:4317");
   })
   ```
   - デバッグ用のコンソール出力
   - OpenTelemetry Collectorへのエクスポート設定

## 環境変数の設定

### appsettings.jsonでの設定

```json
{
  "OpenTelemetry": {
    "ServiceName": "TodoApi",
    "ServiceVersion": "1.0.0",
    "OtlpExporter": {
      "Endpoint": "http://otel-collector:4317"
    }
  }
}
```

### Docker環境での設定

```yaml
services:
  todo-api:
    environment:
      - OTEL_EXPORTER_OTLP_ENDPOINT=http://otel-collector:4317
      - OTEL_SERVICE_NAME=todo-api
```

## 動作確認

1. アプリケーションの起動
   ```bash
   docker compose up -d
   ```

2. コンソール出力の確認
   ```bash
   docker compose logs -f todo-api
   ```

3. 期待される出力例
   ```
   TodoApi[TracerProvider] - Traces
   Activity.TraceId:       abcd1234...
   Activity.SpanId:        efgh5678...
   Activity.TraceFlags:    Recorded
   Activity.ActivitySourceName: TodoApi
   Activity.DisplayName:   GET /api/TodoItems
   Activity.Kind:         Server
   Activity.StartTime:    2025-04-25T17:12:44Z
   Activity.Duration:     100ms
   Activity.Tags:
       http.method: GET
       http.scheme: http
       http.target: /api/TodoItems
   ```

## トラブルシューティング

### よくある問題と解決方法

1. Collectorに接続できない
   - エンドポイントの設定を確認
   - ネットワーク接続を確認
   - Collectorのログを確認

2. トレースが記録されない
   - ソース名が正しく設定されているか確認
   - サンプリング設定を確認
   - デバッグ用のコンソール出力を確認

3. メトリクスが収集されない
   - メーターの設定を確認
   - エクスポーターの設定を確認

## 次のステップ

基本的な設定が完了したら、次は自動計装とカスタム計装の実装に進みます。自動計装で得られる情報と、カスタムで追加する情報を適切に組み合わせることで、より詳細な可観測性を実現していきます。
