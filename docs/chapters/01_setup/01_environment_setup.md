# 開発環境のセットアップ

この章では、Todo アプリケーションの開発環境をセットアップする手順を説明します。

## Dev Container環境の構築

本プロジェクトでは、Dev Container を使用して開発環境を統一しています。これにより、開発者間での環境の差異を最小限に抑え、スムーズな開発を実現します。

### 必要なツール

開発を始める前に、以下のツールをインストールしてください：

1. [Visual Studio Code](https://code.visualstudio.com/)
2. [Docker Desktop](https://www.docker.com/products/docker-desktop/)
3. Visual Studio Code の Dev Containers 拡張機能

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

### Docker Compose 設定

プロジェクトでは以下のサービスを Docker Compose で管理しています：

1. WebAPI (.NET 6)
2. フロントエンド (React)
3. データベース (PostgreSQL)
4. 監視ツール群（次章以降で追加）

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

## 動作確認

環境のセットアップが完了したら、以下の手順で動作確認を行います：

1. データベースの接続確認
```bash
docker compose exec db psql -U postgres -c "\l"
```

2. WebAPI の起動確認
```bash
curl http://localhost:5000/api/health
```

3. フロントエンドの確認
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
- 環境変数の確認
- データベースコンテナの状態確認
```bash
docker compose ps
docker compose logs db
```

3. Dev Container のビルドエラー
- Docker Desktop が起動していることを確認
- Docker キャッシュのクリア
```bash
docker builder prune
```

## 次のステップ

環境のセットアップが完了したら、[バックエンドAPIの実装](./02_backend_implementation.md)に進みます。
