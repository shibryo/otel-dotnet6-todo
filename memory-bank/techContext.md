# 技術コンテキスト

## 技術スタック

### アプリケーション層

1. バックエンド

   - .NET 6 Web API
   - Entity Framework Core
   - PostgreSQL 15
   - RESTful API 設計

2. フロントエンド
   - React
   - 最小限の UI 実装
   - HTTP クライアント

### 可観測性層

1. OpenTelemetry

   - OpenTelemetry SDK (.NET)
   - 自動計装（Auto-instrumentation）
   - カスタム計装
   - Context Propagation

2. モニタリングスタック
   - OpenTelemetry Collector
   - Jaeger (トレース可視化)
   - Grafana (メトリクス監視)

## 開発環境

### コンテナ化環境

- Dev Container
  - mcr.microsoft.com/devcontainers/dotnet:6.0
  - 開発ツール一式

### ローカル開発ツール

- Tilt

  - Docker compose のオーケストレーション
  - ホットリロード対応
  - マルチコンテナ管理

- Docker Compose
  - アプリケーションサービス
  - データベース
  - 監視サービス群

## 技術制約

### アプリケーション制約

1. パフォーマンス

   - レスポンスタイム: < 500ms
   - データベースクエリ最適化
   - 適切なキャッシング戦略

2. スケーラビリティ
   - ステートレスな API 設計
   - 水平スケーリング対応
   - 非同期処理の活用

### 可観測性要件

1. トレーシング

   - 全リクエストの追跡
   - エラーコンテキストの収集
   - パフォーマンスボトルネックの特定

2. メトリクス

   - 基本的なアプリケーションメトリクス
   - カスタムビジネスメトリクス
   - リソース使用率の監視

3. ログ
   - 構造化ログ
   - コンテキスト情報の付加
   - 適切なログレベル管理

## セキュリティ要件

- HTTPS 通信の強制
- クロスオリジンリソース共有（CORS）の適切な設定
- SQL Injection の防止
- 適切な例外処理とエラーメッセージ

## 開発プラクティス

1. コーディング規約

   - C#コーディング規約準拠
   - クリーンアーキテクチャの原則
   - DRY プリンシパル

2. テスト戦略

   - 単体テスト（必須）
   - 統合テスト（重要なフロー）
   - エンドツーエンドテスト（主要機能）

3. バージョン管理
   - GitFlow
   - 意味のあるコミットメッセージ
   - プルリクエストレビュー

## ドキュメント要件

- API 仕様書（OpenAPI/Swagger）
- セットアップガイド
- トラブルシューティングガイド
- アーキテクチャ図
