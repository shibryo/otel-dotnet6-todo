# 第3章：可観測性の実装

## 章の構成

1. [可観測性の基本設定](01_observability_setup.md)
   - 可観測性の3つの柱（トレース、メトリクス、ログ）
   - Docker Composeによる環境構築
   - OpenTelemetry SDKの基本設定
   - ベースラインの監視設定

2. [OpenTelemetry Collectorの設定](02_collector_config.md)
   - Collectorの基本設定
   - エラーハンドリングの実装
   - エラー検出と記録
   - エラーリカバリと再試行

3. [トレース可視化とサンプリング](03_trace_visualization.md)
   - Jaegerによるトレース可視化
   - サンプリング設定の最適化
   - トレース分析とモニタリング
   - トラブルシューティング

4. [メトリクス監視とアラート](04_metrics_monitoring.md)
   - カスタムメトリクスの実装
   - パフォーマンスメトリクスの収集
   - アラート設定
   - パフォーマンス分析

## 学習目標

1. 可観測性の基本概念とツールの理解
2. OpenTelemetry Collectorの設定方法の習得
3. 効果的なトレース可視化とサンプリングの実装
4. メトリクス監視とアラート設定の実践

## 前提知識

- .NET 6の基本的な理解
- Docker Composeの基本的な使用経験
- RESTful APIの基本的な理解

## 環境要件

- .NET 6 SDK
- Docker Desktop
- Visual Studio Code
- HTTPクライアント（Postman、cURL等）

## 補足資料

- OpenTelemetry公式ドキュメント
- Jaegerドキュメント
- Grafanaドキュメント
- Prometheusドキュメント
