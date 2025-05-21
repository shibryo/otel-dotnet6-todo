# フロントエンドの実装

このセクションでは、TodoアプリケーションのフロントエンドをReactを使用して実装します。

## 開発環境の準備

### Tiltfileの設定

`Tiltfile`に以下の設定を追加します：

```python
# フロントエンドサービスの設定
dc_resource('web', 
    deps=['./todo-web/src'],
    trigger_mode=TRIGGER_MODE_AUTO)

# ホットリロードの設定
docker_compose('docker-compose.yml')
```

### 必要なパッケージのインストール

```bash
# コンテナ内でパッケージをインストール
docker compose exec web npm install @mui/material @emotion/react @emotion/styled axios
```

## プロジェクト構造の整備

以下のような構造でファイルを作成します：

```
src/
├── api/
│   └── todoApi.ts      # API クライアント
├── components/
│   ├── TodoForm.tsx    # Todo作成フォーム
│   └── TodoList.tsx    # Todo一覧表示
├── types/
│   └── todo.ts         # 型定義
└── App.tsx             # メインコンポーネント
```

[以下、コンポーネントの実装コードは変更なし・省略]

## 開発サーバーの設定

### Vite設定の更新

`vite.config.ts`を以下のように更新します：

```typescript
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  server: {
    host: '0.0.0.0',
    port: 3000,
    watch: {
      usePolling: true
    }
  },
});
```

## アプリケーションの起動と開発

### 1. 開発環境の起動

```bash
# 環境の起動
tilt up

# ログの確認
docker compose logs -f web
```

### 2. リアルタイムな開発

```bash
# コードの変更を監視
docker compose logs -f web

# ホットリロードの動作確認
docker compose exec web npm run build
```

### 3. コンポーネントのテスト

ブラウザで http://localhost:3000 にアクセスし、以下を確認：
- [ ] Todo項目の追加
- [ ] 一覧表示の更新
- [ ] 完了状態の切り替え
- [ ] 項目の削除

## トラブルシューティング

### 1. ビルドの問題

```bash
# ビルドログの確認
docker compose logs -f web

# node_modulesの再インストール
docker compose exec web rm -rf node_modules
docker compose exec web npm install
```

### 2. API接続の問題

```bash
# バックエンドの状態確認
docker compose logs -f api

# ネットワーク接続の確認
docker compose exec web curl api:5000/api/health

# CORSの確認
docker compose logs api | grep -i cors
```

### 3. 開発サーバーの問題

```bash
# プロセスの確認
docker compose exec web ps aux | grep node

# ポートの使用状況
docker compose exec web netstat -tulpn

# 設定の確認
docker compose exec web cat vite.config.ts
```

> 💡 効果的なデバッグ方法
> - ブラウザのDevToolsでネットワークタブを確認
> - docker composeのログで詳細を確認
> - 複数のサービスのログを同時に監視

## 開発のヒント

### 1. 効率的な開発フロー

```bash
# 変更の監視
docker compose logs -f web

# TypeScriptのエラーチェック
docker compose exec web npm run type-check

# リントの実行
docker compose exec web npm run lint
```

### 2. デバッグの設定

1. ブラウザのDevTools
- Networkタブでリクエスト/レスポンスの確認
- ConsoleタブでTypeScriptエラーの確認
- Reactコンポーネントの検証

2. VS Code設定
```json
{
  "debug.javascript.usePreview": true,
  "debug.javascript.autoAttachFilter": "always"
}
```

### 3. パフォーマンス最適化

```bash
# ビルドサイズの確認
docker compose exec web npm run build
docker compose exec web du -h dist/

# バンドル分析
docker compose exec web npm run build -- --analyze
```

## 次のステップ

フロントエンドの実装が完了したら、[動作確認とトラブルシューティング](./04_testing_and_troubleshooting.md)に進みます。
