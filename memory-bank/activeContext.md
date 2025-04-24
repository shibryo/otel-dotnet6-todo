# アクティブコンテキスト

## 現在の作業状態

### 完了した作業

- プロジェクトの基本要件の定義
- 製品コンテキストの確立
- 技術スタックの選定
- システムパターンの定義
- DevContainer 環境の基本設定
  - devcontainer.json
  - docker-compose.yml（PostgreSQL, Jaeger含む）
  - Dockerfile
- Tilt 環境のセットアップ
- プロジェクト構造の作成
  - ソリューションの作成
  - プロジェクト間の依存関係設定
  - 必要なパッケージのインストール
- Domain層の実装
  - Todoエンティティ
  - ドメイン例外
  - リポジトリインターフェース
- Application層の実装
  - DTOs（TodoDto, CreateTodoDto, UpdateTodoDto, TodoFilterDto）
  - Commands（CreateTodo, UpdateTodo, DeleteTodo）
  - Queries（GetTodoById, GetTodos）
  - Validation（CreateTodoValidator, UpdateTodoValidator）
  - 共通クラス（Result<T>）
- データベース初期化スクリプトの作成

### 進行中の作業

- OpenTelemetryの設定
- テストの実装

### 完了した作業

- API層の実装
  - TodoControllerの実装
  - エラーハンドリング
  - Swagger設定
  - OpenTelemetry基本設定

## 次のステップ

### 1. API層の実装

1. コントローラー
   - TodoControllerの実装
   - エラーハンドリング
   - バリデーション

2. ミドルウェア
   - 例外ハンドリング
   - ログ記録
   - 認証/認可（必要に応じて）

3. Swagger設定
   - APIドキュメント
   - 例示データ

### 2. OpenTelemetryの設定
   - トレース設定
   - メトリクス設定
   - ログ設定

### 3. テストの実装

1. ユニットテスト
   - TodoControllerの実装
   - エラーハンドリング
   - バリデーション

2. ミドルウェア
   - 例外ハンドリング
   - ログ記録
   - 認証/認可（必要に応じて）

3. Swagger設定
   - APIドキュメント
   - 例示データ

### 3. テスト実装

1. ユニットテスト
   - ドメインロジックのテスト
   - コマンド/クエリハンドラーのテスト
   - バリデーションのテスト

2. インテグレーションテスト
   - リポジトリのテスト
   - APIエンドポイントのテスト
   - データベース操作のテスト

3. E2Eテスト
   - シナリオテスト
   - パフォーマンステスト

## 優先度の高い課題

1. EF CoreとPostgreSQLの統合
2. OpenTelemetryの適切な設定
3. テストカバレッジの確保

## リスク管理

1. 技術的なリスク
   - EF Core最適化
   - N+1問題の防止
   - パフォーマンスボトルネック

2. 品質管理
   - テストカバレッジ
   - コードの保守性
   - エラーハンドリングの網羅性

## 決定事項

1. アーキテクチャ
   - CQRSパターンの採用
   - MediatRによるメッセージング
   - FluentValidationによる入力検証

2. データアクセス
   - EF Coreの採用
   - リポジトリパターン
   - 非同期処理の徹底

3. 監視と可観測性
   - OpenTelemetryの採用
   - Jaegerによるトレース可視化
   - 構造化ログ
