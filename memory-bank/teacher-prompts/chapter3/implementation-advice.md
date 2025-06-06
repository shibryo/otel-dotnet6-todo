# 第3章：観測環境の構築 - 実装アドバイス

## 指導方針

### 1. 理解度の確認
各実装ステップで以下の質問を投げかけ：
```
現在の実装について：
- 実装の目的は明確ですか？
- 分からない点や気になる点はありますか？
- より詳しく知りたい部分はありますか？
```

### 2. 疑問点の掘り下げ
よくある疑問：
- なぜこのポート番号を使用するのか
- なぜこのサービスが必要なのか
- サービス間はどのように通信しているのか
- この設定が何を制御しているのか

### 3. 理解の深化
特に注目すべき点：
- コンテナ間の通信の仕組み
- データの流れ（トレース・メトリクス）
- 各ツールの役割と連携

## 対話のポイント

### 1. Collector設定時
確認したい点：
- OTLPプロトコルについて
- レシーバーとエクスポーターの関係
- バッチ処理の意味

### 2. Jaeger連携時
理解を深めたい点：
- トレースデータの流れ
- サンプリングの仕組み
- UI表示までの経路

### 3. Prometheus設定時
掘り下げたい点：
- スクレイピングの仕組み
- メトリクス形式の理解
- データ保持期間

### 4. Grafana設定時
確認したい点：
- データソースの連携
- クエリの書き方
- 可視化の選択

## トラブルシューティングガイド

### 質問すべきポイント
1. エラー発生時
   - エラーメッセージの内容は理解できましたか？
   - どのタイミングでエラーが発生しましたか？
   - 関連するログは確認できましたか？

2. 期待通りに動作しない時
   - 想定している動作は何ですか？
   - 現在の状態をどのように確認しましたか？
   - 設定内容は意図通りですか？

## 学習の進め方

### 1. 概念理解
以下の点について理解を確認：
- 分散トレーシングの目的
- メトリクス収集の意義
- 可視化の重要性

### 2. 実装時
各ステップで以下を確認：
- 実装の目的の理解
- 設定内容の意図
- 動作確認方法

### 3. 動作確認時
確認ポイント：
- データの流れの把握
- 各ツールの連携状態
- 可視化結果の解釈

## フィードバックのポイント

### 1. 実装面
- 意図した通りに動作しているか
- 設定の意図は理解できているか
- 改善したい点はあるか

### 2. 理解面
- 概念は理解できているか
- 不明点は残っていないか
- さらに深めたい部分はあるか

### 3. 運用面
- 実際の運用をイメージできているか
- トラブル時の対応は理解できているか
- スケーリングについて考慮できているか

## 次のステップに向けて

### 確認すべきポイント
- 基本概念の理解度
- 実装の完了度
- 残された疑問点

### 発展的な話題
- パフォーマンスの最適化
- セキュリティの考慮
- 大規模環境での運用
