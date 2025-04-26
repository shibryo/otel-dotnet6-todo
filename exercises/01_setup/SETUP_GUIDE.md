# OpenTelemetry学習環境セットアップガイド

## 1. 前提条件のインストール

### 必須ツール
1. [Visual Studio Code](https://code.visualstudio.com/)
2. [Docker Desktop](https://www.docker.com/products/docker-desktop/)

### VS Code拡張機能
1. Dev Containers拡張機能
2. C# Dev Kit
3. Docker
4. GitLens
5. Markdown All in One

## 2. プロジェクトのセットアップ

1. プロジェクトのクローン
```bash
git clone [repository-url]
cd [project-directory]
```

2. Visual Studio Codeでプロジェクトを開く
```bash
code .
```

3. Dev Container環境の起動
   - VS Codeの左下の「><」アイコンをクリック
   - 「Reopen in Container」を選択
   - 環境のビルドが完了するまで待機

## 3. 開発環境の構成

### ポート設定
- API サーバー: 5000
- Web フロントエンド: 80
- PostgreSQL: 5432
- pgAdmin: 5050

### 環境変数の設定
- データベース接続情報は docker-compose.yml で設定済み
- 追加の環境変数が必要な場合は .env ファイルを作成

### ネットワーク設定
1. Docker Composeネットワーク
   - サービス間の通信はサービス名で解決
   - 例：APIサーバーは `http://todo-api:5000` でアクセス

2. ポートフォワーディング
   - Dev Container環境で自動設定
   - ホストマシンからのアクセスが可能

## 4. アプリケーションの起動

1. Docker Composeでサービスを起動
```bash
cd src/start
docker-compose up -d
```

2. 動作確認
   - APIサーバー: http://localhost:5000/swagger
   - フロントエンド: http://localhost
   - pgAdmin: http://localhost:5050
     - Email: admin@example.com
     - Password: admin

## 5. 開発の開始

### バックエンド開発
- ソースコード: `src/start/TodoApi/`
- 主要なファイル:
  - Program.cs: アプリケーションのエントリーポイント
  - Controllers/: APIエンドポイント
  - Models/: データモデル
  - Data/: DBコンテキスト

### フロントエンド開発
- ソースコード: `src/start/todo-web/`
- 主要なファイル:
  - src/App.tsx: メインコンポーネント
  - src/api/: APIクライアント
  - src/components/: UIコンポーネント

## 6. データベース操作

### マイグレーションの実行
```bash
cd src/start/TodoApi
dotnet ef database update
```

### 新しいマイグレーションの作成
```bash
dotnet ef migrations add <MigrationName>
```

## 7. 各章の進め方

1. 該当する章のドキュメントを確認
   - `docs/chapters/XX_chapter/`を参照

2. 演習の実施
   - `exercises/XX_chapter/`の手順に従って実装

3. 動作確認
   - 各章末の確認項目に沿ってテスト
   - 問題があれば[トラブルシューティングガイド](./TROUBLESHOOTING.md)を参照

## 8. コード管理

### ブランチ運用
- `main`: 初期状態
- `your-work`: 作業用ブランチ
- `chapters/*`: 各章の完了状態
- `solutions`: 完成形のリファレンス実装

### コミットの作成
- 各章の作業が完了したらコミット
- コミットメッセージは章番号を含める
- 例：「Chapter 1: 環境構築とTodoアプリの基本実装完了」

## 9. ヘルプとサポート

### ドキュメント参照
- README.md: プロジェクト概要
- docs/: 詳細な実装ガイド
- memory-bank/: プロジェクトの状態管理

### 問題解決
1. [トラブルシューティングガイド](./TROUBLESHOOTING.md)を確認
2. 未解決の場合は講師（AI）に質問
3. 必要に応じてGitHubのIssueを作成
