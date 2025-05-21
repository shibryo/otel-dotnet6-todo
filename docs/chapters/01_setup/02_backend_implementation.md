# バックエンドAPIの実装

このセクションでは、TodoアプリケーションのバックエンドAPIを実装します。

## プロジェクトの設定

### 開発環境の準備

1. Tiltfileの設定
```python
# APIサービスのビルドと実行
docker_compose('docker-compose.yml')

# ホットリロードの設定
dc_resource('api',
    deps=['./TodoApi'],
    trigger_mode=TRIGGER_MODE_AUTO)
```

2. 必要なパッケージの追加
```bash
docker compose exec api dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
docker compose exec api dotnet add package Microsoft.EntityFrameworkCore.Design
```

## モデルの実装

### TodoItemモデルの作成

`Models/TodoItem.cs`を作成し、以下の内容を実装します：

```csharp
namespace TodoApi.Models;

public class TodoItem
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public bool IsComplete { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

## データベースコンテキストの実装

### DbContextの作成

`Data/TodoContext.cs`を作成し、以下の内容を実装します：

```csharp
using Microsoft.EntityFrameworkCore;
using TodoApi.Models;

namespace TodoApi.Data;

public class TodoContext : DbContext
{
    public TodoContext(DbContextOptions<TodoContext> options)
        : base(options)
    {
    }

    public DbSet<TodoItem> TodoItems { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TodoItem>()
            .Property(t => t.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}
```

## コントローラーの実装

`Controllers/TodoItemsController.cs`を実装します（コード内容は変更なし）。

## アプリケーション設定

### appsettings.Development.jsonの設定

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=db;Database=todos;Username=postgres;Password=postgres"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### Program.csの設定

`Program.cs`の実装（コード内容は変更なし）。

## データベースマイグレーション

1. マイグレーションの作成と適用
```bash
# 開発環境の起動
tilt up

# コンテナ内でマイグレーションを実行
docker compose exec api dotnet ef migrations add InitialCreate
docker compose exec api dotnet ef database update

# マイグレーションの確認
docker compose exec db psql -U postgres -d todos -c "\dt"
```

## 動作確認

### 1. アプリケーションの起動状態確認

```bash
# APIのログ確認
docker compose logs -f api

# データベースの状態確認
docker compose exec db psql -U postgres -d todos -c "SELECT count(*) FROM todo_items"
```

### 2. APIエンドポイントのテスト

```bash
# Todo項目の作成
curl -X POST http://localhost:5000/api/TodoItems \
     -H "Content-Type: application/json" \
     -d '{"title":"テストタスク","isComplete":false}'

# Todo一覧の取得
curl http://localhost:5000/api/TodoItems
```

### 3. SwaggerUIでの確認
- ブラウザで http://localhost:5000/swagger にアクセス

## トラブルシューティング

### 1. データベース接続の問題

```bash
# DBコンテナの状態確認
docker compose logs -f db

# DB接続の確認
docker compose exec db pg_isready

# テーブルの確認
docker compose exec db psql -U postgres -d todos -c "\dt"
```

### 2. アプリケーションの問題

```bash
# アプリケーションログの確認
docker compose logs -f api

# 詳細なログの表示
docker compose logs --tail=100 api | grep -i error

# 環境変数の確認
docker compose exec api env | grep ASPNETCORE
```

### 3. マイグレーションの問題

```bash
# マイグレーション履歴の確認
docker compose exec api dotnet ef migrations list

# マイグレーションのリセット
docker compose exec api dotnet ef database drop -f
docker compose exec api dotnet ef database update
```

> 💡 効果的なデバッグのポイント
> - ログは`docker compose logs`で確認
> - 問題の切り分けは個別のサービスから
> - エラー時は関連サービスのログも確認

## 開発のヒント

### ホットリロードの活用

1. コードの変更を監視
```bash
# Tiltのステータス確認
tilt status

# リアルタイムログの確認
docker compose logs -f api
```

2. デバッグログの有効化
```json
// appsettings.Development.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Debug"
    }
  }
}
```

### データベース操作のヒント

```bash
# PostgreSQLへの直接接続
docker compose exec db psql -U postgres -d todos

# テーブル構造の確認
docker compose exec db psql -U postgres -d todos -c "\d+ todo_items"
```

## 次のステップ

バックエンドAPIの実装が完了したら、[フロントエンドの実装](./03_frontend_implementation.md)に進みます。
