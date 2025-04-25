# バックエンドAPIの実装

このセクションでは、TodoアプリケーションのバックエンドAPIを実装します。

## プロジェクトの作成

### WebAPIプロジェクトの作成

1. プロジェクトの作成
```bash
dotnet new webapi -n TodoApi
cd TodoApi
```

2. 必要なパッケージの追加
```bash
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Microsoft.EntityFrameworkCore.Design
```

## モデルの実装

### TodoItemモデルの作成

`Models/TodoItem.cs`を作成し、以下の内容を実装します：

```csharp
namespace TodoApi.Models;

public class TodoItem
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public bool IsComplete { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

## データベースコンテキストの実装

### DbContextの作成

`Data/TodoContext.cs`を作成し、以下の内容を実装します：

```csharp
using Microsoft.EntityFrameworkCore;
using TodoApi.Models;

namespace TodoApi.Data;

public class TodoContext : DbContext
{
    public TodoContext(DbContextOptions<TodoContext> options)
        : base(options)
    {
    }

    public DbSet<TodoItem> TodoItems { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TodoItem>()
            .Property(t => t.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}
```

## コントローラーの実装

### TodoItemsControllerの作成

`Controllers/TodoItemsController.cs`を作成し、以下の内容を実装します：

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Models;

namespace TodoApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TodoItemsController : ControllerBase
{
    private readonly TodoContext _context;

    public TodoItemsController(TodoContext context)
    {
        _context = context;
    }

    // GET: api/TodoItems
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TodoItem>>> GetTodoItems()
    {
        return await _context.TodoItems.ToListAsync();
    }

    // GET: api/TodoItems/5
    [HttpGet("{id}")]
    public async Task<ActionResult<TodoItem>> GetTodoItem(int id)
    {
        var todoItem = await _context.TodoItems.FindAsync(id);

        if (todoItem == null)
        {
            return NotFound();
        }

        return todoItem;
    }

    // POST: api/TodoItems
    [HttpPost]
    public async Task<ActionResult<TodoItem>> PostTodoItem(TodoItem todoItem)
    {
        todoItem.CreatedAt = DateTime.UtcNow;
        _context.TodoItems.Add(todoItem);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTodoItem), new { id = todoItem.Id }, todoItem);
    }

    // PUT: api/TodoItems/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutTodoItem(int id, TodoItem todoItem)
    {
        if (id != todoItem.Id)
        {
            return BadRequest();
        }

        todoItem.UpdatedAt = DateTime.UtcNow;
        _context.Entry(todoItem).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!TodoItemExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    // DELETE: api/TodoItems/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTodoItem(int id)
    {
        var todoItem = await _context.TodoItems.FindAsync(id);
        if (todoItem == null)
        {
            return NotFound();
        }

        _context.TodoItems.Remove(todoItem);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool TodoItemExists(int id)
    {
        return _context.TodoItems.Any(e => e.Id == id);
    }
}
```

## アプリケーション設定

### appsettings.jsonの設定

`appsettings.json`に以下のデータベース接続設定を追加します：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=db;Database=todos;Username=postgres;Password=postgres"
  },
  // ... 他の設定
}
```

### Program.csの設定

`Program.cs`を以下のように更新します：

```csharp
using Microsoft.EntityFrameworkCore;
using TodoApi.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<TodoContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS設定の追加
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthorization();
app.MapControllers();

// データベースマイグレーションの自動適用
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TodoContext>();
    db.Database.Migrate();
}

app.Run();
```

## データベースマイグレーション

1. 初期マイグレーションの作成
```bash
dotnet ef migrations add InitialCreate
```

2. マイグレーションの適用
```bash
dotnet ef database update
```

## 動作確認

1. アプリケーションの起動
```bash
dotnet run
```

2. SwaggerUIでAPIの確認
- ブラウザで https://localhost:5001/swagger にアクセス
- 各エンドポイントのテスト実行

3. curlでのテスト
```bash
# Todo項目の作成
curl -X POST https://localhost:5001/api/TodoItems \
     -H "Content-Type: application/json" \
     -d '{"title":"テストタスク","isComplete":false}'

# Todo一覧の取得
curl https://localhost:5001/api/TodoItems
```

## トラブルシューティング

### よくある問題と解決方法

1. データベース接続エラー
- 接続文字列の確認
- PostgreSQLコンテナの起動確認
- ネットワーク設定の確認

2. マイグレーションエラー
```bash
# マイグレーションの削除
dotnet ef migrations remove

# DBのドロップ
dotnet ef database drop
```

3. コンパイルエラー
- 必要なパッケージの確認
- using ステートメントの確認
- 構文エラーの修正

## 次のステップ

バックエンドAPIの実装が完了したら、[フロントエンドの実装](./03_frontend_implementation.md)に進みます。
