# otel-dotnet6-todo

このプロジェクトは dotnet6 を使用した todo アプリ主に CRUD を実現し、リクエストを opentelemetry で監視することを確認する。

## API エンドポイント

### Todo作成

```bash
curl -X POST http://localhost:5000/api/todo \
  -H "Content-Type: application/json" \
  -d '{
    "title": "買い物に行く",
    "description": "牛乳と卵を買う",
    "dueDate": "2025-04-25T15:00:00Z"
  }'
```

レスポンス:
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "title": "買い物に行く",
  "description": "牛乳と卵を買う",
  "isCompleted": false,
  "dueDate": "2025-04-25T15:00:00Z",
  "createdAt": "2025-04-24T16:02:04.123456Z",
  "updatedAt": "2025-04-24T16:02:04.123456Z"
}
```

### Todo更新

```bash
curl -X PUT http://localhost:5000/api/todo/3fa85f64-5717-4562-b3fc-2c963f66afa6 \
  -H "Content-Type: application/json" \
  -d '{
    "title": "買い物に行く",
    "description": "牛乳と卵とパンを買う",
    "isCompleted": true,
    "dueDate": "2025-04-25T15:00:00Z"
  }'
```

### Todo削除

```bash
curl -X DELETE http://localhost:5000/api/todo/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

### Todo取得（単一）

```bash
curl http://localhost:5000/api/todo/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

### Todo一覧取得

全件取得:
```bash
curl http://localhost:5000/api/todo
```

フィルター例:
```bash
# 完了済みのTodoのみ
curl "http://localhost:5000/api/todo?isCompleted=true"

# 期限切れのTodoのみ
curl "http://localhost:5000/api/todo?isOverdue=true"

# 特定の期限以前のTodoのみ
curl "http://localhost:5000/api/todo?dueBefore=2025-04-25T00:00:00Z"

# キーワード検索
curl "http://localhost:5000/api/todo?searchTerm=買い物"
```

## get started

## プロジェクトの要件

Devcontainer を使用して、Dotnet6 の環境で開発を行う。
Todo を記録するために使用する DB は Postgres15 である。
アプリの実行時には Docker Compose を使用し、起動するアプリは以下のアプリである。

- Dotnet6 の Todo を記録する CRUD サーバ
- Curl を使用して Todo アプリに所定の操作を行うインテグレーション用のクライアント
- Jaeger
- OTLP

ただし Docker Compose を起動する時には Tilt と呼ばれるマイクロサービス開発環境を利用する。

クライアントのユースケースとして、TODO を追加、TODO を完了、TODO を削除することをテストで確認できること。

わからない単語や自信のないライブラリがあれば必ず調べること。

## プロジェクトの構造

プロジェクトはオニオンアーキテクチャとして、domain,app,infra,view とエントリーポイントを持つものとする。

## プロジェクトの進め方

重要: 私は長期的な記憶ができないため、必ずメモリバンクを用意してタスクの状態管理更新を行い、他のエージェントに代わっても開発を継続できる状態にすること

1. 仕様を詳細化する。
2. 仕様をタスクに分解する。ただし、細かくタスク完了できること
3. タスクを順番に実行する
