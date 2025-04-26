# 第1章：環境構築とTodoアプリの基本実装

## 目的

この章では、OpenTelemetryを学習するための基盤となるTodoアプリケーションを実装します。

## 前提条件

- .NET 6 SDK
- Docker Desktop
- Visual Studio Code
  - C# Dev Kit拡張機能
  - Docker拡張機能

## 実装ステップ

1. プロジェクトの作成
   ```bash
   # プロジェクトの作成
   dotnet new webapi -n TodoApi
   cd TodoApi

   # 必要なパッケージの追加
   dotnet add package Microsoft.EntityFrameworkCore.Design --version 6.0.0
   dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL --version 6.0.0
   ```

2. モデルの実装
   ```csharp
   // Models/TodoItem.cs
   namespace TodoApi.Models;

   public class TodoItem
   {
       public int Id { get; set; }
       public string Title { get; set; } = string.Empty;
       public bool IsComplete { get; set; }
       public DateTime CreatedAt { get; set; }
       public DateTime? CompletedAt { get; set; }
       public string Priority { get; set; } = "normal";
   }
   ```

3. DbContextの実装
   ```csharp
   // Data/TodoContext.cs
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
   }
   ```

4. コントローラーの実装
   ```csharp
   // Controllers/TodoItemsController.cs
   using Microsoft.AspNetCore.Mvc;
   using Microsoft.EntityFrameworkCore;
   using TodoApi.Data;
   using TodoApi.Models;

   namespace TodoApi.Controllers;

   [ApiController]
   [Route("api/[controller]")]
   public class TodoItemsController : ControllerBase
   {
       private readonly TodoContext _context;

       public TodoItemsController(TodoContext context)
       {
           _context = context;
       }

       [HttpGet]
       public async Task<ActionResult<IEnumerable<TodoItem>>> GetTodoItems()
       {
           return await _context.TodoItems.ToListAsync();
       }

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

       [HttpPost]
       public async Task<ActionResult<TodoItem>> PostTodoItem(TodoItem todoItem)
       {
           todoItem.CreatedAt = DateTime.UtcNow;
           _context.TodoItems.Add(todoItem);
           await _context.SaveChangesAsync();
           return CreatedAtAction(nameof(GetTodoItem), new { id = todoItem.Id }, todoItem);
       }

       [HttpPut("{id}")]
       public async Task<IActionResult> PutTodoItem(int id, TodoItem todoItem)
       {
           if (id != todoItem.Id)
           {
               return BadRequest();
           }

           if (todoItem.IsComplete && !todoItem.CompletedAt.HasValue)
           {
               todoItem.CompletedAt = DateTime.UtcNow;
           }
           else if (!todoItem.IsComplete)
           {
               todoItem.CompletedAt = null;
           }

           _context.Entry(todoItem).State = EntityState.Modified;
           await _context.SaveChangesAsync();
           return NoContent();
       }

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
   }
   ```

5. Program.csの設定
   ```csharp
   using Microsoft.EntityFrameworkCore;
   using TodoApi.Data;

   var builder = WebApplication.CreateBuilder(args);

   // Add services to the container.
   builder.Services.AddControllers();
   builder.Services.AddEndpointsApiExplorer();
   builder.Services.AddSwaggerGen();

   // Add CORS
   builder.Services.AddCors(options =>
   {
       options.AddDefaultPolicy(policy =>
       {
           policy.WithOrigins("http://localhost:3000")
                 .AllowAnyHeader()
                 .AllowAnyMethod();
       });
   });

   // Add DbContext
   builder.Services.AddDbContext<TodoContext>(options =>
       options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

   var app = builder.Build();

   // Configure the HTTP request pipeline.
   if (app.Environment.IsDevelopment())
   {
       app.UseSwagger();
       app.UseSwaggerUI();
   }

   // Enable CORS
   app.UseCors();

   app.UseAuthorization();
   app.MapControllers();

   app.Run();
   ```

6. appsettings.jsonの設定
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=db;Database=todo_db;Username=postgres;Password=postgres"
     },
     "Logging": {
       "LogLevel": {
         "Default": "Information",
         "Microsoft.AspNetCore": "Warning"
       }
     },
     "AllowedHosts": "*"
   }
   ```

7. マイグレーションの作成と実行
   ```bash
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   ```

8. Dockerfileの作成
   ```dockerfile
   FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
   WORKDIR /src
   COPY ["TodoApi.csproj", "./"]
   RUN dotnet restore
   COPY . .
   RUN dotnet publish -c Release -o /app

   FROM mcr.microsoft.com/dotnet/aspnet:6.0
   WORKDIR /app
   COPY --from=build /app .
   ENTRYPOINT ["dotnet", "TodoApi.dll"]
   ```

## 動作確認手順

1. アプリケーションの起動
   ```bash
   cd src/start
   docker-compose up -d
   ```

2. APIの動作確認
   - ブラウザで http://localhost:5000/swagger にアクセス
   - SwaggerUIから各エンドポイントをテスト
   
   または、以下のcurlコマンドでテスト：
   ```bash
   # Todo項目の作成
   curl -X POST http://localhost:5000/api/TodoItems \
        -H "Content-Type: application/json" \
        -d '{"title":"Test Todo","isComplete":false,"priority":"normal"}'

   # Todo項目の一覧取得
   curl http://localhost:5000/api/TodoItems
   ```

## 次のステップ

基本的なTodoアプリケーションの実装が完了したら、次章でOpenTelemetryの導入を行います。
