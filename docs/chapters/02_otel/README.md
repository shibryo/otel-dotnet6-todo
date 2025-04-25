# 第2章: OpenTelemetryの導入

## 概要

この章では、TodoアプリケーションにOpenTelemetryを導入し、分散トレーシングとメトリクス収集を実装します。基本的な概念から実践的な実装まで、段階的に学習を進めていきます。

## 学習目標

1. OpenTelemetryの基本概念を理解する
   - Traces, Metrics, Logsの概念
   - Context Propagationの仕組み
   - Samplingの考え方

2. .NET 6でのOpenTelemetry実装方法を習得する
   - OpenTelemetry SDKの設定
   - 自動計装の活用
   - カスタム計装の実装
   - メトリクスの収集と可視化

## 前提条件

- 第1章の環境構築が完了していること
- TodoアプリケーションのCRUD機能が実装済みであること
- Dev Container環境が正常に動作していること

## 章の構成

1. [OpenTelemetryの基本](01_otel_basics.md)
   - OpenTelemetryの概要
   - 主要コンポーネントの説明
   - 基本的な用語と概念

2. [SDKのインストールと設定](02_sdk_installation.md)
   - 必要なパッケージの追加
   - SDKの初期化設定
   - 基本設定の解説

3. [自動計装とカスタム計装](03_instrumentation.md)
   - ASP.NET Coreの自動計装
   - Entity Framework Coreの自動計装
   - HTTPクライアントの自動計装
   - カスタムSpanの実装
   - エラーハンドリングの計装

4. [メトリクスとログの実装](04_metrics_and_logging.md)
   - メトリクスの設計と実装
   - カスタムメトリクスの追加
   - 構造化ログの実装
   - コンテキスト情報の付加

## 期待される成果

この章を完了することで、以下のスキルを習得できます：

1. OpenTelemetryの基本的な概念と用語の理解
2. .NET 6アプリケーションでのOpenTelemetry実装方法
3. 自動計装とカスタム計装の使い分け
4. 効果的なメトリクス設計とログ実装

## 注意事項

- コードの可読性を重視し、実践的な実装パターンを学習します
- 各ステップで動作確認を行い、理解を深めていきます
- トラブルシューティングのポイントも併せて解説します
