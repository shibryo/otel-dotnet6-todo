# OpenTelemetry SDKのインストール

このセクションでは、TodoアプリケーションにOpenTelemetry SDKをインストールし、基本的な設定を行います。

## 開発環境の準備

### 1. Tiltfileの更新

```python
# OpenTelemetry SDKの変更を監視
dc_resource('api',
    deps=['./TodoApi'],
    trigger_mode=TRIGGER_MODE_AUTO)

# アプリケーションの再起動を自動化
dc_resource('api',
    resource_deps=['db'],
    trigger_mode=TRIGGER_MODE_AUTO)
```

### 2. 必要なパッケージの追加

TodoApiプロジェクトに以下のパッケージを追加します：

```bash
# プロジェクトディレクトリで実行
cd src/start/TodoApi
dotnet add package OpenTelemetry
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package OpenTelemetry.Instrumentation.AspNetCore
dotnet add package OpenTelemetry.Instrumentation.Http
dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol
```

> 💡 パッケージはプロジェクトファイルに直接追加する
> - コンテナ内ではなくホストマシンで実行
> - 変更を永続化するため
> - 再起動時も設定が維持される

## OpenTelemetry SDKの設定

### 1. Program.csの更新

```csharp
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// OpenTelemetryの設定
builder.Services.AddOpenTelemetry()
    .WithTracing(builder => builder
        .SetResourceBuilder(ResourceBuilder.CreateDefault()
            .AddService("todo-api"))
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter());
```

### 2. 設定の確認

```bash
# アプリケーションの再起動
docker compose restart api

# ログの確認
docker compose logs -f api | grep -i opentelemetry
```

## 動作確認

### 1. トレース出力の確認

```bash
# テストリクエストの送信
curl -X POST http://localhost:5000/api/todoitems \
  -H "Content-Type: application/json" \
  -d '{"title":"OpenTelemetryのテスト","isComplete":false}'

# ログの確認
docker compose logs -f api | grep -i trace
```

### 2. エクスポートの確認

```bash
# OTLPエクスポーターの設定確認
docker compose exec api env | grep OTEL

# ネットワーク接続の確認
docker compose exec api nc -zv otelcol 4317
```

## トラブルシューティング

### 1. SDKの問題

```bash
# パッケージの参照確認
docker compose exec api dotnet list package | grep OpenTelemetry

# アセンブリの読み込み確認
docker compose exec api dotnet run --list-modules
```

### 2. 設定の問題

```bash
# 環境変数の確認
docker compose exec api env | grep OTEL

# 設定ファイルの確認
docker compose exec api cat appsettings.Development.json
```

### 3. ランタイムの問題

```bash
# アプリケーションログの確認
docker compose logs -f api

# デバッグレベルのログ出力
docker compose exec api env ASPNETCORE_ENVIRONMENT=Development
```

## 開発のヒント

### 1. デバッグモードの活用

```bash
# デバッグログの有効化
docker compose exec api env OTEL_LOG_LEVEL=debug

# トレースの詳細出力
docker compose exec api env OTEL_TRACES_SAMPLER=always_on
```

### 2. ホットリロードの活用

```bash
# コードの変更を監視
docker compose logs -f api

# 変更の即時反映を確認
curl http://localhost:5000/health
```

> 💡 効果的なデバッグのポイント
> - ログレベルを適切に設定
> - サンプリング率を開発時は100%に
> - エクスポート先の疎通を確認

## コンポーネントのカスタマイズ

### 1. カスタムTraceProviderの追加

```csharp
// カスタム設定の例
builder.Services.AddOpenTelemetry()
    .WithTracing(builder => builder
        .SetResourceBuilder(ResourceBuilder.CreateDefault()
            .AddService("todo-api")
            .AddAttributes(new KeyValuePair<string, object>[]
            {
                new("environment", "development"),
                new("version", "1.0.0")
            }))
        .AddAspNetCoreInstrumentation(options =>
        {
            options.RecordException = true;
            options.EnrichWithException = (activity, exception) =>
            {
                activity.SetTag("error.type", exception.GetType().Name);
                activity.SetTag("error.message", exception.Message);
            };
        }));
```

### 2. サンプリング設定

```bash
# サンプリング率の設定
docker compose exec api env OTEL_TRACES_SAMPLER_ARG=0.5

# サンプリング結果の確認
docker compose logs -f api | grep -i sampling
```

## 次のステップ

SDKのインストールと基本設定が完了したら、[計装の実装](./03_instrumentation.md)に進みます。
