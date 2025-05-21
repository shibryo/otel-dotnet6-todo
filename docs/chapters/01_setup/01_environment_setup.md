# 開発環境のセットアップ

この章では、Todo アプリケーションの開発環境をセットアップする手順を説明します。

## Dev Container環境の構築

本プロジェクトでは、Dev Container を使用して開発環境を統一しています。これにより、開発者間での環境の差異を最小限に抑え、スムーズな開発を実現します。

### 必要なツール

開発を始める前に、以下のツールをインストールしてください：

1. [Visual Studio Code](https://code.visualstudio.com/)
2. [Docker Desktop](https://www.docker.com/products/docker-desktop/)
3. [Tilt](https://tilt.dev/)
4. Visual Studio Code の Dev Containers 拡張機能

### プロジェクトの開始

1. プロジェクトをクローン
```bash
git clone [repository-url]
cd [project-directory]
```

2. Visual Studio Code でプロジェクトを開く
```bash
code .
```

3. Dev Container を起動する
   - VS Code の左下の「><」アイコンをクリック
   - 「Reopen in Container」を選択
   - Dev Container のビルドが開始され、環境が準備されます

## プロジェクト構成

プロジェクトは以下のような構成になっています：

```
src/
├── TodoApi/               # バックエンドAPI
│   ├── Controllers/      # APIコントローラー
│   ├── Models/          # データモデル
│   ├── Data/           # DBコンテキスト
│   └── ...
└── todo-web/            # フロントエンド
    ├── src/            # ソースコード
    ├── public/         # 静的ファイル
    └── ...
```

## 開発環境の構成

### Tiltfile の設定

プロジェクトでは以下のサービスをTiltで管理しています：

```python
# Tiltfile
# Docker Compose環境の定義
docker_compose('docker-compose.yml')

# 依存関係の定義
dc_resource('db')
dc_resource('api', deps=['db'])
dc_resource('web', deps=['api'])

# ログの設定
dc_resource('api', 
  resource_deps=['db'],
  trigger_mode=TRIGGER_MODE_AUTO)
```

### Docker Compose 設定

```yaml
# docker-compose.yml の主要な設定
services:
  api:
    build: 
      context: ./TodoApi
    ports:
      - "5000:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      
  web:
    build: 
      context: ./todo-web
    ports:
      - "3000:3000"
      
  db:
    image: postgres:15
    environment:
      POSTGRES_PASSWORD: postgres
    ports:
      - "5432:5432"
```

## 環境の起動と動作確認

1. 環境の起動
```bash
# 開発環境の起動
tilt up

# 環境の停止（終了時）
tilt down
```

2. データベースの接続確認
```bash
# ログの確認
docker compose logs -f db

# PostgreSQLへの接続確認
docker compose exec db psql -U postgres -c "\l"
```

3. WebAPI の起動確認
```bash
# APIのログ確認
docker compose logs -f api

# ヘルスチェック
curl http://localhost:5000/api/health
```

4. フロントエンドの確認
```bash
# Webサーバーのログ確認
docker compose logs -f web
```
- ブラウザで http://localhost:3000 にアクセス

## トラブルシューティング

### よくある問題と解決方法

1. ポートが既に使用されている場合
```bash
# 使用中のポートを確認
sudo lsof -i :<port-number>

# プロセスの終了
sudo kill <PID>
```

2. データベース接続エラー
```bash
# 環境変数の確認
docker compose exec api env | grep DB_

# データベースコンテナのログ確認
docker compose logs -f db

# データベースの状態確認
docker compose exec db pg_isready
```

3. コンテナの動作確認
```bash
# 全コンテナの状態確認
docker compose ps

# 特定サービスのログ確認
docker compose logs -f [service-name]

# エラーログの抽出
docker compose logs --tail=100 | grep -i error
```

> 💡 効果的なデバッグ
> - Tilt UIでリアルタイムな状態確認
> - docker composeのログで詳細確認
> - エラー時は関連サービスのログを横断的に確認

## 次のステップ

環境のセットアップが完了したら、[バックエンドAPIの実装](./02_backend_implementation.md)に進みます。
