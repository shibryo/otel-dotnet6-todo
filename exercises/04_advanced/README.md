# 第4章：高度な機能と運用

## 目的

この章では、OpenTelemetryの高度な機能を実装し、より効果的な監視と運用を実現します。

## 前提条件

- 第3章までの実装が完了していること
- OpenTelemetryの基本概念を理解していること
- 監視とアラートの基本を理解していること

## 実装ステップ

1. サンプリングプロセッサーの実装
   ```csharp
   // Sampling/TodoSamplingProcessor.cs
   using OpenTelemetry;
   using OpenTelemetry.Trace;
   using System.Diagnostics;

   namespace TodoApi.Sampling;

   public class TodoSamplingProcessor : BaseProcessor<Activity>
   {
       private readonly double _defaultSamplingRatio;
       private readonly HashSet<string> _importantEndpoints;

       public TodoSamplingProcessor(double defaultSamplingRatio = 0.1)
       {
           _defaultSamplingRatio = defaultSamplingRatio;
           _importantEndpoints = new HashSet<string>
           {
               "/api/TodoItems/Create",
               "/api/TodoItems/Delete"
           };
       }

       public override void OnStart(Activity activity)
       {
           if (activity == null) return;

           // 重要なエンドポイントは常にサンプリング
           if (IsImportantEndpoint(activity))
           {
               activity.ActivityTraceFlags |= ActivityTraceFlags.Recorded;
               return;
           }

           // エラーがある場合は常にサンプリング
           if (HasError(activity))
           {
               activity.ActivityTraceFlags |= ActivityTraceFlags.Recorded;
               return;
           }

           // デフォルトのサンプリング率を適用
           if (Random.Shared.NextDouble() < _defaultSamplingRatio)
           {
               activity.ActivityTraceFlags |= ActivityTraceFlags.Recorded;
           }
           else
           {
               activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
           }
       }

       private bool IsImportantEndpoint(Activity activity)
       {
           var httpRoute = activity.GetTagItem("http.route") as string;
           return !string.IsNullOrEmpty(httpRoute) && 
                  _importantEndpoints.Contains(httpRoute);
       }

       private bool HasError(Activity activity)
       {
           var statusCode = activity.GetTagItem("http.status_code") as string;
           return !string.IsNullOrEmpty(statusCode) && 
                  (statusCode.StartsWith("4") || statusCode.StartsWith("5"));
       }
   }
   ```

2. カスタムメトリクスの拡張
   ```csharp
   // Metrics/TodoMetrics.cs に以下のメトリクスを追加
   private readonly Counter<int> _todoOperationErrorCounter;
   private readonly Histogram<double> _apiResponseTimeHistogram;

   public void RecordOperationError(string operation, string errorType)
   {
       _todoOperationErrorCounter.Add(1, 
           new KeyValuePair<string, object>[] 
           {
               new("operation", operation),
               new("error_type", errorType)
           });
   }

   public void RecordApiResponseTime(double milliseconds, string operation)
   {
       _apiResponseTimeHistogram.Record(milliseconds, 
           new KeyValuePair<string, object>[] 
           {
               new("operation", operation)
           });
   }
   ```

3. アラートルールの設定（Prometheus）
   ```yaml
   # docker/prometheus.yml に追加
   groups:
   - name: todo_alerts
     rules:
     # エラー率アラート
     - alert: HighErrorRate
       expr: rate(todo_operation_errors_total[5m]) > 0.1
       for: 5m
       labels:
         severity: warning
       annotations:
         summary: "高いエラー率を検出"
         description: "直近5分間のエラー率が10%を超えています"

     # レスポンスタイムアラート
     - alert: HighResponseTime
       expr: histogram_quantile(0.95, rate(todo_api_response_time_bucket[5m])) > 500
       for: 5m
       labels:
         severity: warning
       annotations:
         summary: "高いレスポンスタイムを検出"
         description: "p95レスポンスタイムが500msを超えています"
   ```

4. パフォーマンス計測の実装
   ```csharp
   // Controllers/TodoItemsController.csの各アクションメソッドを更新
   public class TodoItemsController : ControllerBase
   {
       private readonly TodoContext _context;
       private readonly TodoMetrics _metrics;
       private readonly Stopwatch _stopwatch;

       public TodoItemsController(TodoContext context, TodoMetrics metrics)
       {
           _context = context;
           _metrics = metrics;
           _stopwatch = new Stopwatch();
       }

       [HttpGet]
       public async Task<ActionResult<IEnumerable<TodoItem>>> GetTodoItems()
       {
           try
           {
               _stopwatch.Start();
               var result = await _context.TodoItems.ToListAsync();
               _metrics.RecordApiResponseTime(_stopwatch.ElapsedMilliseconds, "GET");
               return result;
           }
           catch (Exception ex)
           {
               _metrics.RecordOperationError("GET", ex.GetType().Name);
               throw;
           }
           finally
           {
               _stopwatch.Reset();
           }
       }
   }
   ```

5. エラーハンドリングの改善
   ```csharp
   // Program.csに追加
   app.UseExceptionHandler(errorApp =>
   {
       errorApp.Run(async context =>
       {
           var error = context.Features.Get<IExceptionHandlerFeature>();
           if (error != null)
           {
               var ex = error.Error;
               var metrics = context.RequestServices.GetService<TodoMetrics>();
               
               // エラー情報の記録
               metrics?.RecordOperationError(
                   context.Request.Path,
                   ex.GetType().Name);

               await context.Response.WriteAsJsonAsync(new
               {
                   error = "An error occurred.",
                   type = ex.GetType().Name,
                   detail = ex.Message
               });
           }
       });
   });
   ```

## 動作確認手順

1. 環境の再起動
   ```bash
   cd src/start
   docker-compose down
   docker-compose up -d --build
   ```

2. エラー状況のテスト
   ```bash
   # 存在しないTodoの取得
   curl http://localhost:5000/api/TodoItems/999

   # 無効なデータでTodoを作成
   curl -X POST http://localhost:5000/api/TodoItems \
        -H "Content-Type: application/json" \
        -d '{}'
   ```

3. パフォーマンステスト
   ```bash
   # 複数のリクエストを同時に実行
   for i in {1..10}; do
     curl http://localhost:5000/api/TodoItems &
   done
   ```

## 監視のポイント

1. Jaegerでのトレース分析
   - エラー発生時のトレース確認
   - サンプリング率の確認
   - 処理時間の分布確認

2. Prometheusでのメトリクス監視
   - エラー率の推移
   - レスポンスタイムの分布
   - リクエスト数の推移

3. Grafanaでのダッシュボード
   - エラー監視パネル
   - パフォーマンスパネル
   - リソース使用率パネル

## 運用上の注意点

1. サンプリング設定
   - 環境に応じた適切な設定
   - 重要な操作の確実な記録
   - エラー時の確実な記録

2. アラート設定
   - 適切な閾値の設定
   - アラートの優先度付け
   - 誤検知の防止

3. パフォーマンス監視
   - 定期的な傾向分析
   - ボトルネックの特定
   - 改善策の検討

## まとめ

この章で学んだ内容：
- 効果的なサンプリング戦略
- 詳細なメトリクス収集
- 適切なアラート設定
- エラー監視とトラブルシューティング
- パフォーマンス分析と改善
