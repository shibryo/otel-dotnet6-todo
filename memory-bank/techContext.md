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

- データベースアクセス
  - Entity Framework Core
  - リポジトリ実装
- OpenTelemetry
  - トレースプロバイダー設定
  - メトリクス収集
  - ログ統合
- 外部サービス連携
  - Jaeger
  - OTLP

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
- Jaeger でのトレース確認
