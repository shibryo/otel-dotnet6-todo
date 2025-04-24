# プロジェクト概要

## 基本要件

- .NET 6 を使用した Todo アプリケーションの開発
- OpenTelemetry を用いたリクエスト監視の実装
- DevContainer 環境での開発
- Tilt を使用したマイクロサービス開発環境

## 技術スタック

- バックエンド: .NET 6
- データベース: PostgreSQL 15
- コンテナ環境: Docker Compose
- 開発環境: DevContainer
- 監視: OpenTelemetry, Jaeger
- デプロイ: Tilt

## アーキテクチャ

オニオンアーキテクチャを採用:

- Domain 層: ビジネスロジックとエンティティ
- Application 層: ユースケース実装
- Infrastructure 層: 外部サービスとの連携
- View 層: API エンドポイント
- エントリーポイント: アプリケーションの起動設定

## コンポーネント構成

1. Todo サーバー API (.NET 6)
2. インテグレーションテスト用クライアント (Curl)
3. 監視システム (Jaeger)
4. テレメトリプロトコル (OTLP)

## 主要機能要件

1. Todo 項目の CRUD 操作
   - Todo 項目の追加
   - Todo 項目の完了
   - Todo 項目の削除
   - Todo 項目の取得
2. OpenTelemetry によるリクエスト監視
3. インテグレーションテストによる機能検証
