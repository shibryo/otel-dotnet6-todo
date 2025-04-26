# トラブルシューティングガイド

## 1. 環境構築時の問題

### Dev Container起動の問題

#### Docker Desktopが起動していない
```
ERROR: Cannot connect to the Docker daemon at unix:///var/run/docker.sock. Is the docker daemon running?
```
**解決方法:**
1. Docker Desktopを起動
2. システムトレイでDockerのステータスを確認
3. 必要に応じてDocker Desktopを再起動

#### ポートが既に使用されている
```
Error starting userland proxy: listen tcp 0.0.0.0:5000: bind: address already in use
```
**解決方法:**
1. 使用中のポートを確認
```bash
sudo lsof -i :5000
```
2. 該当のプロセスを終了
```bash
sudo kill <PID>
```

#### メモリ不足エラー
```
Build failed: executor failed running [/bin/sh -c ...]: failed to allocate memory
```
**解決方法:**
1. Docker Desktopの設定を開く
2. Resources > Memoryの値を増やす（推奨: 4GB以上）
3. Docker Desktopを再起動

### データベース接続の問題

#### 接続エラー
```
A connection attempt failed because the connected party did not properly respond...
```
**解決方法:**
1. Docker Composeサービスの状態確認
```bash
docker compose ps
```
2. ログの確認
```bash
docker compose logs db
```
3. データベースコンテナの再起動
```bash
docker compose restart db
```

#### マイグレーションエラー
```
Unable to create an object of type 'TodoContext'...
```
**解決方法:**
1. 接続文字列の確認
```bash
docker compose exec todo-api cat appsettings.json
```
2. データベースの存在確認
```bash
docker compose exec db psql -U postgres -c "\l"
```
3. マイグレーションのリセット
```bash
docker compose exec todo-api dotnet ef database drop --force
docker compose exec todo-api dotnet ef database update
```

## 2. アプリケーション実行時の問題

### APIサーバーの問題

#### 500エラー
```
500 Internal Server Error
```
**解決方法:**
1. ログの確認
```bash
docker compose logs todo-api
```
2. デバッグモードでの実行
   - VS Codeのデバッガーをアタッチ
   - ブレークポイントを設定して処理を確認

#### CORS エラー
```
Access to fetch at 'http://localhost:5000/api/...' has been blocked by CORS policy
```
**解決方法:**
1. Program.csのCORS設定を確認
2. フロントエンドのAPI呼び出しURLを確認
3. 必要に応じてCORS設定を更新

### フロントエンドの問題

#### ビルドエラー
```
Module not found: Error: Can't resolve ...
```
**解決方法:**
1. 依存関係のインストール
```bash
docker compose exec todo-web npm install
```
2. node_modulesの削除と再インストール
```bash
docker compose exec todo-web rm -rf node_modules
docker compose exec todo-web npm install
```

#### APIリクエストの失敗
```
Failed to fetch...
```
**解決方法:**
1. APIサーバーの起動確認
2. ネットワーク設定の確認
3. APIクライアントの設定確認（src/api/todoApi.ts）

## 3. パフォーマンスの問題

### コンテナの応答が遅い

**解決方法:**
1. リソース使用状況の確認
```bash
docker stats
```
2. 不要なコンテナの停止
```bash
docker compose stop <service-name>
```
3. Docker Desktopのリソース設定見直し

### データベースのパフォーマンス

**解決方法:**
1. PostgreSQLログの確認
```bash
docker compose exec db tail -f /var/log/postgresql/postgresql-*.log
```
2. インデックスの確認と最適化
3. 必要に応じてPostgreSQLの設定調整

## 4. 開発環境のリセット

### 完全なリセット手順

1. コンテナとボリュームの削除
```bash
docker compose down -v
```

2. イメージの削除
```bash
docker compose rm -f
docker system prune -a
```

3. 環境の再構築
```bash
docker compose up -d --build
```

4. データベースの再作成
```bash
docker compose exec todo-api dotnet ef database update
```

## 5. その他の問題

### GitのLFS関連エラー
```
Smudge error: Error downloading object...
```
**解決方法:**
1. Git LFSの初期化
```bash
git lfs install
```
2. オブジェクトのフェッチ
```bash
git lfs fetch --all
git lfs pull
```

### VS Code拡張機能の問題
**解決方法:**
1. VS Codeの再読み込み（Command + Shift + P > Reload Window）
2. 拡張機能の再インストール
3. Dev Containerの再ビルド

## サポート

問題が解決しない場合：
1. このガイドの手順を試す
2. プロジェクトのIssueを確認
3. 講師（AI）に質問
4. 新しいIssueを作成（再現手順を詳細に記載）
