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
            .AddSource("TodoApi")
            .SetResourceBuilder(
                ResourceBuilder.CreateDefault()
                    .AddService(serviceName: "TodoApi", serviceVersion: "1.0.0"))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation()
            .AddProcessor(new TodoSamplingProcessor(
                defaultSamplingRatio: builder.Environment.IsDevelopment() ? 1.0 : 0.1))
            .AddConsoleExporter()
            .AddOtlpExporter(opts => {
                opts.Endpoint = new Uri("http://otel-collector:4317");
            })
    })
    .WithMetrics(metricsProviderBuilder =>
    {
        metricsProviderBuilder
            .SetResourceBuilder(
                ResourceBuilder.CreateDefault()
                    .AddService(serviceName: "TodoApi", serviceVersion: "1.0.0"))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddConsoleExporter()
            .AddOtlpExporter(opts => {
                opts.Endpoint = new Uri("http://otel-collector:4317");
            });
    });

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

// データベースマイグレーションの自動適用
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TodoContext>();
    db.Database.Migrate();
}

app.Run();
