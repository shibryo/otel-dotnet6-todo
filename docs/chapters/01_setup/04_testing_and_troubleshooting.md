# 動作確認とトラブルシューティング

このセクションでは、実装したTodoアプリケーションの動作確認方法と、発生する可能性のある問題の解決方法を説明します。

## 環境の起動確認

### 1. Tiltによる環境の起動

```bash
# 開発環境の起動
tilt up

# 環境の停止（終了時）
tilt down
```

### 2. サービスの状態確認

```bash
# 全サービスの状態確認
docker compose ps

# 個別のログ確認
docker compose logs -f api    # バックエンドのログ
docker compose logs -f web    # フロントエンドのログ
docker compose logs -f db     # データベースのログ

# エラーログの確認
docker compose logs --tail=100 | grep -i error
```

> 💡 ログ確認のポイント
> - エラーメッセージの確認
> - サービス間の依存関係の問題
> - タイミングに関する問題

## APIエンドポイントのテスト

### 1. Swagger UIでのテスト

1. ブラウザで http://localhost:5000/swagger にアクセス
2. 各エンドポイントの動作を確認：
   - GET /api/TodoItems
   - POST /api/TodoItems
   - PUT /api/TodoItems/{id}
   - DELETE /api/TodoItems/{id}

### 2. curlコマンドでのテスト

```bash
# Todo項目の作成
curl -X POST http://localhost:5000/api/TodoItems \
  -H "Content-Type: application/json" \
  -d '{"title":"テストタスク","isComplete":false}'

# Todo一覧の取得
curl http://localhost:5000/api/TodoItems

# APIの状態確認
curl http://localhost:5000/health
```

## フロントエンドの動作確認

### 1. 開発サーバーの確認

```bash
# フロントエンドのログ監視
docker compose logs -f web

# ビルド状態の確認
docker compose exec web npm run build -- --watch
```

### 2. ブラウザでの動作確認

1. http://localhost:3000 にアクセス
2. 以下の機能をテスト：
   - Todo項目の追加
   - 一覧表示の更新
   - 完了状態の切り替え
   - 項目の削除

## トラブルシューティング

### 1. サービス起動の問題

```bash
# コンテナの状態確認
docker compose ps

# リソース使用状況の確認
docker stats

# ネットワーク接続の確認
docker network inspect $(docker compose ps -q)
```

### 2. データベース関連の問題

```bash
# DB接続の確認
docker compose exec db pg_isready

# テーブルの確認
docker compose exec db psql -U postgres -d todos -c "\dt"

# マイグレーションの状態確認
docker compose exec api dotnet ef migrations list
```

### 3. API接続の問題

```bash
# APIの疎通確認
docker compose exec web curl api:5000/health

# CORSエラーの確認
docker compose logs api | grep -i cors

# ネットワーク設定の確認
docker compose exec api env | grep ASPNETCORE
```

### 4. フロントエンドの問題

```bash
# node_modulesの再構築
docker compose exec web rm -rf node_modules
docker compose exec web npm install

# TypeScriptのエラー確認
docker compose exec web npm run type-check

# ビルドキャッシュのクリア
docker compose exec web npm run build -- --force
```

## 効率的なデバッグ方法

### 1. 複数サービスのログ監視

```bash
# すべてのサービスのログを表示
docker compose logs -f

# 特定のキーワードでフィルタ
docker compose logs -f | grep -i error
docker compose logs -f | grep -i warn
```

### 2. コンテナ内のデバッグ

```bash
# APIコンテナ内でのデバッグ
docker compose exec api dotnet --info
docker compose exec api dotnet --list-sdks

# フロントエンドコンテナ内でのデバッグ
docker compose exec web node --version
docker compose exec web npm list
```

### 3. パフォーマンスの確認

```bash
# リソース使用状況の監視
docker stats

# ネットワークの確認
docker network inspect $(docker compose ps -q)
```

## 動作確認チェックリスト

### バックエンド
- [ ] APIサーバーが起動している
- [ ] データベース接続が確立している
- [ ] マイグレーションが適用されている
- [ ] エンドポイントが応答している

### フロントエンド
- [ ] 開発サーバーが起動している
- [ ] APIと通信できている
- [ ] コンポーネントが正しく描画されている
- [ ] イベントハンドリングが機能している

### 統合テスト
- [ ] E2Eフローが機能している
- [ ] データの永続化が確認できる
- [ ] エラーハンドリングが適切

## 次のステップ

基本的なTodoアプリケーションの実装と動作確認が完了したら、第2章「OpenTelemetryの導入」に進みます。次章では、このアプリケーションに分散トレーシングを実装していきます。
