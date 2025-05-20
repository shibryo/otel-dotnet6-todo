using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using TodoApi.Metrics;
using System.Text.Json;
using TodoApi.Logging;

var builder = WebApplication.CreateBuilder(args);

// ログ設定の追加
builder.Services.AddLogging(logging =>
{
    logging.AddJsonConsole(options =>
    {
        options.IncludeScopes = true;
        options.TimestampFormat = "yyyy-MM-dd HH:mm:ss";
        options.JsonWriterOptions = new System.Text.Json.JsonWriterOptions
        {
            Indented = true
        };
        options.UseUtcTimestamp = true;
    });
});

// OpenTelemetryの設定を追加
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
        tracerProviderBuilder
            .AddSource("TodoApi.Traces")
            .SetResourceBuilder(
                ResourceBuilder.CreateDefault()
                    .AddService(serviceName: "TodoApi", serviceVersion: "1.0.0")
                    .AddAttributes(new Dictionary<string, object>
                    {
                        ["environment"] = builder.Environment.EnvironmentName
                    }))
            .AddAspNetCoreInstrumentation()    // WebAPI自動計装
            .AddEntityFrameworkCoreInstrumentation()  // EF Core自動計装
            .AddOtlpExporter())
    .WithMetrics(metricsBuilder => 
        metricsBuilder
            .AddMeter("TodoApi.Metrics")
            .AddAspNetCoreInstrumentation()  // HTTP メトリクスを追加
            .AddPrometheusExporter()
            .AddOtlpExporter());

// Add services to the container.
builder.Services.AddSingleton<TodoMetrics>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost")
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

// Prometheusメトリクスのエンドポイントを有効化
app.UseOpenTelemetryPrometheusScrapingEndpoint();

// データベースマイグレーションの自動適用
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TodoContext>();
    db.Database.Migrate();
}

app.Run();
