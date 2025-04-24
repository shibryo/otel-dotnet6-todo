# 技術コンテキスト

## 開発環境

### DevContainer 設定

- .NET 6 SDK
- PostgreSQL 15
- Docker Compose
- Tilt
- 必要な拡張機能
  - C# Dev Kit
  - Docker
  - PostgreSQL

### データベース

- PostgreSQL 15
- マイグレーション管理
- Entity Framework Core

### ビルド・デプロイ

- Tilt
  - マイクロサービスの管理
  - ホットリロード
  - リソースの依存関係管理

## アーキテクチャ詳細

### Domain 層

- エンティティ
  - Todo
    - ID (GUID)
    - タイトル (string)
    - 説明 (string)
    - 作成日時 (DateTime)
    - 完了日時 (DateTime?)
    - 削除日時 (DateTime?)
    - ステータス (enum)
- 値オブジェクト
- ドメインサービス
- リポジトリインターフェース

### Application 層

- ユースケース
  - TodoItem の作成
  - TodoItem の完了
  - TodoItem の削除
  - TodoItem の取得
- DTO マッピング
- バリデーション
- トランザクション管理

### Infrastructure 層

#### データベースアクセス
1. Entity Framework Core 6.0
   - DbContext実装（TodoDbContext）
   - エンティティ設定（TodoConfiguration）
   - マイグレーション管理
   - デザインタイムDbContextFactory

2. リポジトリ実装
   - ITodoRepositoryの実装
   - 非同期操作の完全サポート
   - キャンセレーショントークン対応
   - 効率的なクエリ実装
     - 完了済みTodoの取得
     - 未完了Todoの取得
     - 期限切れTodoの取得
     - タイトルでの検索（ILike）

3. 依存関係の設定
   - サービスコレクション拡張メソッド
   - スコープ管理
   - 接続文字列の注入

#### 可観測性基盤
1. OpenTelemetry実装
   - トレーシング
     ```csharp
     // 実装パッケージ
     - OpenTelemetry.Extensions.Hosting
     - OpenTelemetry.Instrumentation.AspNetCore
     - OpenTelemetry.Instrumentation.EntityFrameworkCore
     - OpenTelemetry.Exporter.Jaeger
     - OpenTelemetry.Exporter.Prometheus.AspNetCore
     ```
   - メトリクス収集
     - カスタムメーター設定
     - Todoアプリケーション固有のメトリクス
     - システムメトリクスの自動収集
   - アクティビティソース
     - HTTPコンテキスト伝播
     - MediatRパイプラインの計装
     - カスタムアクティビティ

2. モニタリングスタック
   - Jaeger (トレース)
     - ポート: 16686 (UI)
     - プロトコル: OTLP (gRPC)
     - 収集設定: サンプリングなし
   - Prometheus (メトリクス)
     - スクレイピング間隔: 15秒
     - エンドポイント: /metrics
     - カスタムメトリクス収集
   - Grafana (可視化)
     - 認証: Anonymous Admin
     - データソース: Prometheus
     - ポート: 3000

### View 層（API）

- コントローラー
  - TodoController
    - POST /api/todos
    - PUT /api/todos/{id}/complete
    - DELETE /api/todos/{id}
    - GET /api/todos
    - GET /api/todos/{id}
- ミドルウェア
  - 例外ハンドリング
  - ロギング
  - トレーシング

## テスト戦略

### ユニットテスト

- Domain 層のテスト
  - エンティティの振る舞い
  - 値オブジェクトの不変条件
- Application 層のテスト
  - ユースケースの実行フロー
  - バリデーションルール

### インテグレーションテスト

- API エンドポイントのテスト
- データベース操作のテスト
- OpenTelemetry の統合テスト

### E2E テスト

- curl を使用したシナリオテスト
- 分散トレーシングの検証
  ```bash
  # トレースの確認
  curl http://localhost:16686
  
  # メトリクスの確認
  curl http://localhost:5001/metrics
  
  # Grafanaダッシュボード
  curl http://localhost:3000
  ```
