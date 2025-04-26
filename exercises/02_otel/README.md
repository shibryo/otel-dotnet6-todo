# 第2章：OpenTelemetryの導入

## 目的

この章では、TodoアプリケーションにOpenTelemetryを導入し、基本的な計装を実装します。

## 前提条件

- 第1章の実装が完了していること
- OpenTelemetryの基本概念を理解していること
  - トレース（Trace）
  - スパン（Span）
  - コンテキスト伝搬（Context Propagation）

## 実装ステップ

1. OpenTelemetryパッケージの追加
   ```bash
   cd TodoApi

   # 基本パッケージ
   dotnet add package OpenTelemetry --version 1.5.0
   dotnet add package OpenTelemetry.Api --version 1.5.0

   # エクスポーター
   dotnet add package OpenTelemetry.Exporter.Console --version 1.5.0
   dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol --version 1.5.0

   # 自動計装
   dotnet add package OpenTelemetry.Extensions.Hosting --version 1.5.0
   dotnet add package OpenTelemetry.Instrumentation.AspNetCore --version 1.5.0-beta.1
   dotnet add package OpenTelemetry.Instrumentation.Http --version 1.5.0-beta.1
   dotnet add package OpenTelemetry.Instrumentation.EntityFrameworkCore --version 1.0.0-beta.7
   ```

2. メトリクスクラスの実装
   ```csharp
   // Metrics/TodoMetrics.cs
   using System.Diagnostics.Metrics;

   namespace TodoApi.Metrics;

   public class TodoMetrics
   {
       private readonly Counter<int> _todosCreatedCounter;
       private readonly Counter<int> _todosCompletedCounter;
       private readonly UpDownCounter<int> _activeTodosCounter;
       private readonly Histogram<double> _todoCompletionTimeHistogram;

       public TodoMetrics()
       {
           var meter = new Meter("TodoApi");
           
           _todosCreatedCounter = meter.CreateCounter<int>(
               "todo.created",
               description: "Number of todo items created");

           _todosCompletedCounter = meter.CreateCounter<int>(
               "todo.completed",
               description: "Number of todo items marked as complete");

           _activeTodosCounter = meter.CreateUpDownCounter<int>(
               "todo.active",
               description: "Number of active (incomplete) todo items");

           _todoCompletionTimeHistogram = meter.CreateHistogram<double>(
               "todo.completion_time",
               unit: "ms",
               description: "Time taken to complete todo items");
       }

       public void TodoCreated()
       {
           _todosCreatedCounter.Add(1);
           _activeTodosCounter.Add(1);
       }

       public void TodoCompleted(DateTime createdAt)
       {
           _todosCompletedCounter.Add(1);
           _activeTodosCounter.Add(-1);
           
           var completionTime = (DateTime.UtcNow - createdAt).TotalMilliseconds;
           _todoCompletionTimeHistogram.Record(completionTime);
       }

       public void TodoUncompleted()
       {
           _activeTodosCounter.Add(1);
       }

       public void TodoDeleted(bool wasCompleted)
       {
           if (!wasCompleted)
           {
               _activeTodosCounter.Add(-1);
           }
       }
   }
   ```

3. OpenTelemetry設定の追加（Program.cs）
   ```csharp
   using OpenTelemetry.Resources;
   using OpenTelemetry.Trace;
   using OpenTelemetry.Metrics;
   using TodoApi.Metrics;

   // Register TodoMetrics as a singleton
   builder.Services.AddSingleton<TodoMetrics>();

   // Configure OpenTelemetry
   builder.Services.AddOpenTelemetry()
       .WithTracing(tracerProviderBuilder =>
       {
           tracerProviderBuilder
               .AddSource("TodoApi")
               .SetResourceBuilder(
                   ResourceBuilder.CreateDefault()
                       .AddService(serviceName: "TodoApi", serviceVersion: "1.0.0"))
               .AddAspNetCoreInstrumentation()
               .AddHttpClientInstrumentation()
               .AddEntityFrameworkCoreInstrumentation()
               .AddConsoleExporter();
       })
       .WithMetrics(metricsProviderBuilder =>
       {
           metricsProviderBuilder
               .SetResourceBuilder(
                   ResourceBuilder.CreateDefault()
                       .AddService(serviceName: "TodoApi", serviceVersion: "1.0.0"))
               .AddAspNetCoreInstrumentation()
               .AddHttpClientInstrumentation()
               .AddConsoleExporter();
       });
   ```

4. コントローラーの更新（メトリクス記録の追加）
   ```csharp
   [ApiController]
   [Route("api/[controller]")]
   public class TodoItemsController : ControllerBase
   {
       private readonly TodoContext _context;
       private readonly TodoMetrics _metrics;

       public TodoItemsController(TodoContext context, TodoMetrics metrics)
       {
           _context = context;
           _metrics = metrics;
       }

       [HttpPost]
       public async Task<ActionResult<TodoItem>> PostTodoItem(TodoItem todoItem)
       {
           todoItem.CreatedAt = DateTime.UtcNow;
           _context.TodoItems.Add(todoItem);
           await _context.SaveChangesAsync();
           
           _metrics.TodoCreated();
           
           return CreatedAtAction(nameof(GetTodoItem), 
               new { id = todoItem.Id }, todoItem);
       }

       [HttpPut("{id}")]
       public async Task<IActionResult> PutTodoItem(int id, TodoItem todoItem)
       {
           if (id != todoItem.Id)
           {
               return BadRequest();
           }

           var existingItem = await _context.TodoItems.FindAsync(id);
           if (existingItem == null)
           {
               return NotFound();
           }

           if (todoItem.IsComplete && !existingItem.IsComplete)
           {
               todoItem.CompletedAt = DateTime.UtcNow;
               _metrics.TodoCompleted(existingItem.CreatedAt);
           }
           else if (!todoItem.IsComplete && existingItem.IsComplete)
           {
               todoItem.CompletedAt = null;
               _metrics.TodoUncompleted();
           }

           _context.Entry(existingItem).CurrentValues.SetValues(todoItem);
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
           
           _metrics.TodoDeleted(todoItem.IsComplete);
           
           return NoContent();
       }
   }
   ```

## 動作確認手順

1. アプリケーションの再起動
   ```bash
   cd src/start
   docker-compose down
   docker-compose up -d --build
   ```

2. トレースとメトリクスの確認
   ```bash
   # コンテナのログを確認
   docker-compose logs -f todo-api
   ```

3. いくつかのTodo操作を実行してトレースを生成
   ```bash
   # Todo項目の作成
   curl -X POST http://localhost:5000/api/TodoItems \
        -H "Content-Type: application/json" \
        -d '{"title":"Test Todo","isComplete":false}'

   # Todo項目の完了
   curl -X PUT http://localhost:5000/api/TodoItems/1 \
        -H "Content-Type: application/json" \
        -d '{"id":1,"title":"Test Todo","isComplete":true}'
   ```

## 確認ポイント

1. 自動計装
   - HTTPリクエストのトレース
   - データベース操作のトレース
   - リクエスト処理時間の計測

2. カスタムメトリクス
   - Todo作成数の記録
   - アクティブなTodo数の追跡
   - 完了までの時間の計測

## 次のステップ

基本的なOpenTelemetryの導入が完了したら、次章で監視環境の構築を行い、トレースの可視化とメトリクスの監視を実装します。
