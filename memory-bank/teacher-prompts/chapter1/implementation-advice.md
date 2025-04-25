# 第1章：実装アドバイス

## WebAPI実装のベストプラクティス

### TodoItemモデルの実装

```csharp
using System.ComponentModel.DataAnnotations;

public class TodoItem
{
    public long Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Title { get; set; } = string.Empty;

    public bool IsComplete { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }
}
```

#### ポイント解説
- `[Required]`属性で必須項目を指定
- `[StringLength]`属性でバリデーション設定
- nullableリファレンス型の活用
- プロパティの適切な初期化

### DbContextの実装

```csharp
using Microsoft.EntityFrameworkCore;

public class TodoContext : DbContext
{
    public TodoContext(DbContextOptions<TodoContext> options)
        : base(options)
    {
    }

    public DbSet<TodoItem> TodoItems { get; set; } = null!;
}
```

#### ポイント解説
- コンストラクタインジェクションの使用
- DbSetプロパティの定義
- null非許容参照型の適切な使用

### コントローラーの実装例

```csharp
[ApiController]
[Route("api/[controller]")]
public class TodoItemsController : ControllerBase
{
    private readonly TodoContext _context;
    private readonly ILogger<TodoItemsController> _logger;

    public TodoItemsController(TodoContext context, ILogger<TodoItemsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/TodoItems
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TodoItem>>> GetTodoItems()
    {
        try
        {
            return await _context.TodoItems.ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TodoItems取得中にエラーが発生しました");
            return StatusCode(500, "内部サーバーエラーが発生しました");
        }
    }
}
```

#### ポイント解説
- 依存性注入の活用
- ロギングの実装
- 非同期メソッドの使用
- 適切な例外処理

## フロントエンド実装のベストプラクティス

### APIクライアントの実装

```typescript
// src/api/todoApi.ts
export interface TodoItem {
  id: number;
  title: string;
  isComplete: boolean;
  description?: string;
}

export const getTodoItems = async (): Promise<TodoItem[]> => {
  const response = await fetch('/api/todoitems');
  if (!response.ok) {
    throw new Error('APIの呼び出しに失敗しました');
  }
  return response.json();
};
```

#### ポイント解説
- 型定義の活用
- エラーハンドリング
- 非同期処理の実装

### コンポーネントの実装

```typescript
// src/components/TodoList.tsx
import React, { useEffect, useState } from 'react';
import { TodoItem, getTodoItems } from '../api/todoApi';

export const TodoList: React.FC = () => {
  const [todos, setTodos] = useState<TodoItem[]>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const loadTodos = async () => {
      try {
        const items = await getTodoItems();
        setTodos(items);
      } catch (err) {
        setError(err instanceof Error ? err.message : '予期せぬエラーが発生しました');
      }
    };

    loadTodos();
  }, []);

  if (error) {
    return <div className="error">{error}</div>;
  }

  return (
    <div>
      <h2>Todo一覧</h2>
      <ul>
        {todos.map(todo => (
          <li key={todo.id}>
            {todo.title} - {todo.isComplete ? '完了' : '未完了'}
          </li>
        ))}
      </ul>
    </div>
  );
};
```

#### ポイント解説
- Hooks（useState, useEffect）の適切な使用
- エラー状態の管理
- 型安全な実装
- キーの適切な使用

## 実装手順

### 1. 環境構築

1. プロジェクトの作成
```bash
dotnet new webapi -n TodoApi
cd TodoApi
```

2. パッケージの追加
```bash
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
```

3. フロントエンドプロジェクトの作成
```bash
npm create vite@latest todo-web -- --template react-ts
cd todo-web
npm install
```

### 2. データベース設定

1. appsettings.json の設定
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=db;Database=todo_db;Username=postgres;Password=postgres"
  }
}
```

2. DbContext の登録（Program.cs）
```csharp
builder.Services.AddDbContext<TodoContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
```

3. マイグレーションの作成と実行
```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 3. APIエンドポイントの実装

1. モデルの作成
2. DbContextの実装
3. コントローラーの実装
4. エンドポイントのテスト

### 4. フロントエンド実装

1. APIクライアントの作成
2. コンポーネントの実装
3. スタイルの適用
4. 動作確認

## よくあるエラーと解決方法

### 1. データベース接続エラー
```
ERROR: connect ECONNREFUSED 127.0.0.1:5432
```

**解決方法**:
1. PostgreSQLコンテナの起動確認
2. 接続文字列のホスト名を確認
3. ポート番号の競合確認

### 2. CORS エラー
```
Access to fetch at 'http://localhost:5000/api/todoitems' from origin 'http://localhost:3000' has been blocked by CORS policy
```

**解決方法**:
```csharp
// Program.cs
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.WithOrigins("http://localhost:3000")
               .AllowAnyHeader()
               .AllowAnyMethod();
    });
});

app.UseCors();
```

### 3. マイグレーションエラー
```
Build failed. Use dotnet build to see the errors.
```

**解決方法**:
1. プロジェクトのビルドエラーを確認
2. マイグレーションファイルの削除
3. 新しいマイグレーションの作成
4. データベースの更新

## デバッグのコツ

### バックエンド

1. ログの活用
```csharp
_logger.LogInformation("Todo item created: {Id}", item.Id);
_logger.LogError(ex, "Error occurred while creating todo item");
```

2. デバッグポイントの設置
- コントローラーのアクションメソッド
- DbContextの操作箇所
- 例外処理の箇所

### フロントエンド

1. React Developer Tools の利用
- コンポーネントの状態確認
- レンダリングの追跡

2. Console.log デバッグ
```typescript
useEffect(() => {
  const loadTodos = async () => {
    try {
      console.log('Fetching todos...');
      const items = await getTodoItems();
      console.log('Fetched todos:', items);
      setTodos(items);
    } catch (err) {
      console.error('Error fetching todos:', err);
      setError(err instanceof Error ? err.message : '予期せぬエラーが発生しました');
    }
  };

  loadTodos();
}, []);
```

## パフォーマンス最適化のヒント

### バックエンド

1. 非同期処理の活用
```csharp
// 良い例
public async Task<ActionResult<IEnumerable<TodoItem>>> GetTodoItems()
{
    return await _context.TodoItems.ToListAsync();
}

// 避けるべき例
public ActionResult<IEnumerable<TodoItem>> GetTodoItems()
{
    return _context.TodoItems.ToList();
}
```

2. インデックスの活用
```csharp
modelBuilder.Entity<TodoItem>()
    .HasIndex(t => t.Title);
```

### フロントエンド

1. メモ化の活用
```typescript
const MemoizedTodoItem = React.memo(({ todo }: { todo: TodoItem }) => (
  <li>
    {todo.title} - {todo.isComplete ? '完了' : '未完了'}
  </li>
));
```

2. 適切なキーの使用
```typescript
{todos.map(todo => (
  <MemoizedTodoItem key={todo.id} todo={todo} />
))}
```

## セキュリティ考慮事項

1. 入力バリデーション
```csharp
public class TodoItem
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    [RegularExpression(@"^[a-zA-Z0-9\s]*$")]
    public string Title { get; set; } = string.Empty;
}
```

2. エラーメッセージのサニタイズ
```csharp
// 良い例
return StatusCode(500, "内部サーバーエラーが発生しました");

// 避けるべき例
return StatusCode(500, ex.ToString());
```

3. CORS設定の適切な管理
```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.WithOrigins("http://localhost:3000")
               .AllowAnyHeader()
               .AllowAnyMethod();
    });
});
