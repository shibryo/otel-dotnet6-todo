# システムパターン

## アーキテクチャパターン

### オニオンアーキテクチャの適用方針

1. 依存関係の方向

   - 内側のレイヤーは外側のレイヤーを知らない
   - すべての依存は内側に向かう
   - インターフェースを用いた依存性の逆転

2. レイヤー間の通信
   - DTO を使用したデータ変換
   - 明示的な境界の定義
   - インターフェースを介した疎結合

### CQRS パターン

1. コマンド（書き込み）

   - 個別のユースケースクラス
   - トランザクション管理
   - 楽観的ロック

2. クエリ（読み取り）
   - 専用のクエリモデル
   - 効率的なデータ取得
   - キャッシュ戦略

## デザインパターン

### リポジトリパターン

1. インターフェース階層
   - IRepository<T>による基本操作の定義
   - ITodoRepositoryによるドメイン固有の操作拡張

2. 実装パターン
   ```csharp
   public interface ITodoRepository : IRepository<Todo>
   {
       Task<IReadOnlyList<Todo>> GetCompletedAsync();
       Task<IReadOnlyList<Todo>> GetIncompleteAsync();
       Task<IReadOnlyList<Todo>> GetDueBeforeAsync(DateTimeOffset date);
       Task<IReadOnlyList<Todo>> GetOverdueAsync();
       Task<IReadOnlyList<Todo>> SearchByTitleAsync(string searchTerm);
       Task<bool> ExistsAsync(Guid id);
   }
   ```

3. 実装の特徴
   - EF Coreとの統合
   - 非同期操作の一貫した使用
   - キャンセレーショントークンのサポート
   - 読み取り専用リストの使用

### ファクトリーパターン

- エンティティ生成の一元管理
- 不変条件の保証
- バリデーション

### ユニットオブワーク

- トランザクション境界の明確化
- 整合性の保証
- 永続化の一貫性

## データアクセスパターン

### Entity Framework Core

1. コード優先アプローチ
   - DbContext定義
     ```csharp
     public class TodoDbContext : DbContext
     {
         public DbSet<Todo> Todos { get; set; }
         
         protected override void OnModelCreating(ModelBuilder modelBuilder)
         {
             modelBuilder.ApplyConfiguration(new TodoConfiguration());
         }
     }
     ```
   - エンティティ設定
     ```csharp
     public class TodoConfiguration : IEntityTypeConfiguration<Todo>
     {
         public void Configure(EntityTypeBuilder<Todo> builder)
         {
             builder.HasKey(t => t.Id);
             builder.Property(t => t.Title).IsRequired().HasMaxLength(255);
             builder.Property(t => t.Description).IsRequired(false);
             // その他のプロパティ設定
         }
     }
     ```
   - マイグレーション管理

2. クエリ最適化
   - ILike関数による大文字小文字を区別しない検索
   - インデックスの適切な設定
   - 必要なデータのみの取得

2. クエリ最適化
   - インデックス戦略
   - 遅延ロード vs 即時ロード
   - クエリのキャッシュ

## テストパターン

### テストピラミッド

1. ユニットテスト (60%)

   - ドメインロジック
   - ユースケース
   - バリデーション

2. インテグレーションテスト (30%)

   - リポジトリ
   - API エンドポイント
   - OpenTelemetry

3. E2E テスト (10%)
   - シナリオベース
   - 重要な機能パス

### テスト戦略

1. DDD

   - 集約の整合性
   - ドメインルールの検証
   - 値オブジェクトの不変条件

2. TDD
   - Red-Green-Refactor
   - 境界値テスト
   - エッジケース

## モニタリングパターン

### OpenTelemetry

1. トレース

   - リクエストの追跡
   - パフォーマンス計測
   - エラー検出

2. メトリクス
   - システムリソース
   - アプリケーションメトリクス
   - ビジネスメトリクス

### ログ戦略

1. 構造化ログ

   - コンテキスト情報
   - 相関 ID
   - エラー詳細

2. ログレベル
   - Info: 通常の操作
   - Warning: 潜在的な問題
   - Error: 実行時エラー
   - Debug: 開発時の詳細
