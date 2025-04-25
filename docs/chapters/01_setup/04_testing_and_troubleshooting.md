# 動作確認とトラブルシューティング

このセクションでは、実装したTodoアプリケーションの動作確認方法と、発生する可能性のある問題の解決方法を説明します。

## 環境の起動確認

### 1. Docker Composeの起動

全てのサービスを起動します：

```bash
docker compose up -d
```

各サービスの状態を確認：

```bash
docker compose ps
```

期待される出力：
```
NAME                SERVICE             STATUS              PORTS
todo-api           api                 running             0.0.0.0:5000->80/tcp
todo-web           web                 running             0.0.0.0:3000->3000/tcp
todo-db            db                  running             0.0.0.0:5432->5432/tcp
```

### 2. ログの確認

各サービスのログを確認：

```bash
# 全てのサービスのログを表示
docker compose logs

# 特定のサービスのログを表示
docker compose logs api
docker compose logs web
docker compose logs db
```

## APIエンドポイントのテスト

### 1. Swagger UIでのテスト

1. ブラウザで http://localhost:5000/swagger にアクセス
2. 各エンドポイントの動作を確認：
   - GET /api/TodoItems
   - POST /api/TodoItems
   - PUT /api/TodoItems/{id}
   - DELETE /api/TodoItems/{id}

### 2. curlコマンドでのテスト

1. Todo項目の作成
```bash
curl -X POST http://localhost:5000/api/TodoItems \
  -H "Content-Type: application/json" \
  -d '{"title":"テストタスク","isComplete":false}'
```

2. Todo一覧の取得
```bash
curl http://localhost:5000/api/TodoItems
```

3. Todo項目の更新
```bash
curl -X PUT http://localhost:5000/api/TodoItems/1 \
  -H "Content-Type: application/json" \
  -d '{"id":1,"title":"更新されたタスク","isComplete":true}'
```

4. Todo項目の削除
```bash
curl -X DELETE http://localhost:5000/api/TodoItems/1
```

## フロントエンドの動作確認

### 1. 基本機能の確認

1. ブラウザで http://localhost:3000 にアクセス
2. 以下の機能をテスト：
   - 新しいTodo項目の追加
   - Todo一覧の表示
   - Todo項目の完了状態の切り替え
   - Todo項目の削除

### 2. ブラウザ開発者ツールでの確認

1. ネットワークタブ
   - APIリクエストの成功確認
   - レスポンスステータスの確認
   - データの形式確認

2. コンソールタブ
   - エラーメッセージの確認
   - 警告メッセージの確認

## 一般的な問題と解決方法

### 1. バックエンド関連の問題

#### データベース接続エラー

症状：
- API呼び出し時に500エラー
- ログに接続エラーが表示

解決方法：
1. 接続文字列の確認
```bash
# appsettings.json の確認
cat TodoApi/appsettings.json

# データベースコンテナの状態確認
docker compose ps db
```

2. データベースの接続テスト
```bash
# データベースコンテナに接続
docker compose exec db psql -U postgres -d todos

# テーブルの確認
\dt
```

#### マイグレーションエラー

症状：
- テーブルが存在しないエラー
- スキーマの不一致エラー

解決方法：
```bash
# マイグレーションの状態確認
dotnet ef migrations list

# マイグレーションの再適用
dotnet ef database drop
dotnet ef database update
```

### 2. フロントエンド関連の問題

#### CORSエラー

症状：
- ブラウザコンソールにCORSエラーが表示
- APIリクエストが失敗

解決方法：
1. バックエンドのCORS設定確認
```csharp
// Program.cs の CORS設定確認
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});
```

2. フロントエンドのAPI_BASE_URLの確認
```typescript
// src/api/todoApi.ts の設定確認
const API_BASE_URL = 'http://localhost:5000/api';
```

#### ビルドエラー

症状：
- `npm run dev` が失敗
- TypeScriptコンパイルエラー

解決方法：
1. 依存パッケージの再インストール
```bash
rm -rf node_modules
npm install
```

2. TypeScript設定の確認
```bash
# tsconfig.json の確認
cat tsconfig.json
```

### 3. Docker関連の問題

#### コンテナ起動エラー

症状：
- コンテナが起動しない
- ポートが既に使用中

解決方法：
1. 既存コンテナの確認と停止
```bash
# 実行中のコンテナ確認
docker ps

# 全コンテナの停止と削除
docker compose down
```

2. ポートの使用状況確認
```bash
# 使用中のポートの確認
sudo lsof -i :5000
sudo lsof -i :3000
```

#### ボリュームの問題

症状：
- データが永続化されない
- マウントエラー

解決方法：
```bash
# ボリュームの確認
docker volume ls

# ボリュームの削除と再作成
docker compose down -v
docker compose up -d
```

## 動作確認チェックリスト

### バックエンド
- [ ] サーバーが起動している
- [ ] Swagger UIにアクセスできる
- [ ] データベースに接続できる
- [ ] CRUD操作が全て成功する

### フロントエンド
- [ ] 開発サーバーが起動している
- [ ] UIが正しく表示される
- [ ] Todo項目を追加できる
- [ ] Todo項目を更新できる
- [ ] Todo項目を削除できる

### 統合テスト
- [ ] フロントエンドからバックエンドへのリクエストが成功する
- [ ] データの永続化が機能している
- [ ] エラーハンドリングが適切に動作する

## 次のステップ

基本的なTodoアプリケーションの実装と動作確認が完了したら、第2章「OpenTelemetryの導入」に進みます。次章では、このアプリケーションに分散トレーシングを実装していきます。
