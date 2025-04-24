using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.OpenApi.Models;
using TodoApp.Api.Extensions;
using TodoApp.Application.Commands;
using TodoApp.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Add MediatR
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(CreateTodo).Assembly);
});

// Add FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(CreateTodo).Assembly);

// Add Infrastructure services
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

app.UseAuthorization();

app.MapControllers();

app.Run();
