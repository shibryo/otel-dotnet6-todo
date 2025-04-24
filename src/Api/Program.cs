using Microsoft.OpenApi.Models;
using TodoApp.Api.Extensions;
using TodoApp.Application;
using TodoApp.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Add Application Layer
builder.Services.AddApplication();

// Add Infrastructure Layer
builder.Services.AddInfrastructure(builder.Configuration);

// Add OpenTelemetry
builder.Services.AddOpenTelemetryServices(builder.Configuration);

// Configure Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "TodoApp API", 
        Version = "v1",
        Description = "A simple Todo application API with OpenTelemetry integration"
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TodoApp API V1");
    });
}

// Global error handling
app.UseExceptionHandler("/error");

// Add Prometheus metrics endpoint
app.MapPrometheusScrapingEndpoint();

app.UseHttpsRedirection();

// Add request logging middleware
app.Use(async (context, next) =>
{
    var startTime = DateTime.UtcNow;
    try
    {
        await next();
    }
    finally
    {
        var elapsed = DateTime.UtcNow - startTime;
        TelemetryConstants.RequestDuration.Record(elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("path", context.Request.Path),
            new KeyValuePair<string, object?>("method", context.Request.Method),
            new KeyValuePair<string, object?>("status", context.Response.StatusCode));
    }
});

app.UseAuthorization();

app.MapControllers();

app.Run();
