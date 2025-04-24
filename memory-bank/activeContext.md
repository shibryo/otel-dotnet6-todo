# アクティブコンテキスト

## 現在の作業状態

### 完了した作業

- OpenTelemetryの実装
  - トレーシング機能
    - Jaegerエクスポーターの設定
    - MediatRパイプラインのトレース
    - コントローラーアクションのトレース
    - EF Coreクエリのトレース
    - カスタムタグとイベントの追加
  - メトリクス機能
    - Prometheusエクスポーターの設定
    - カスタムメトリクスの実装（Todoアイテム作成数、完了数）
    - リクエスト処理時間の測定
    - ASP.NET CoreとEF Coreの標準メトリクス
  - モニタリングスタック
    - Prometheusサーバーの設定
    - Grafanaダッシュボードの準備
    - 分散トレーシング（Jaeger）の統合

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
- API層の実装
  - TodoControllerの実装
  - エラーハンドリング
  - Swagger設定
  - OpenTelemetry基本設定
- ユニットテストの実装
  - Domain層のテスト
    - Todoエンティティのテスト（作成、更新、完了状態の管理）
  - Application層のテスト
    - Commands
      - CreateTodoのテスト（正常系、バリデーション）
      - UpdateTodoのテスト（正常系、存在しないID、バリデーション）
      - DeleteTodoのテスト（正常系、存在しないID）
    - Queries
      - GetTodoByIdのテスト（正常系、存在しないID）
      - GetTodosのテスト（フィルタリング、ソート）
    - Validation
      - CreateTodoValidatorのテスト（タイトル、期限日のバリデーション）
      - UpdateTodoValidatorのテスト（タイトル、期限日、完了状態のバリデーション）

### 進行中の作業

- インテグレーションテストの実装

## 次のステップ

### 1. モニタリングの強化
- Grafanaダッシュボードの作成
  - リクエスト処理時間のグラフ
  - Todoアイテムの作成/完了率
  - エラーレート
- アラートルールの設定
  - 高レイテンシー
  - エラー率上昇
  - リソース使用率

### 2. テスト実装

1. インテグレーションテスト
   - リポジトリのテスト
     - TodoRepositoryのCRUD操作
     - フィルタリング機能
   - APIエンドポイントのテスト
     - TodoController各エンドポイント
   - データベース操作のテスト
     - マイグレーション
     - トランザクション

2. E2Eテスト
   - シナリオテスト
   - パフォーマンステスト

### 3. Infrastructure層の完成
- EF CoreとPostgreSQLの統合
- マイグレーションの作成
- シードデータの準備

## 優先度の高い課題

1. EF CoreとPostgreSQLの統合
2. OpenTelemetryの適切な設定
3. インテグレーションテストの実装

## リスク管理

1. 技術的なリスク
   - EF Core最適化
   - N+1問題の防止
   - パフォーマンスボトルネック

2. 品質管理
   - インテグレーションテストのカバレッジ
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
   - OpenTelemetryによる完全な可観測性
     - トレース：Jaegerによる分散トレーシング
     - メトリクス：Prometheusによる収集と可視化
     - ログ：構造化ログとトレースの関連付け
   - カスタムメトリクス
     - ビジネスメトリクス（Todo作成数、完了率）
     - 技術メトリクス（レイテンシー、エラー率）
   - モニタリングスタック
     - Prometheus：メトリクス収集
     - Grafana：可視化とアラート
     - Jaeger：分散トレーシング
