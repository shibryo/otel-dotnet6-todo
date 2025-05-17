using Microsoft.Extensions.Logging;

namespace TodoApi.Logging;


public static class LogEvents
{
    // CRUD操作のイベント 
    public static readonly EventId TodoCreated = new(1000, "TodoCreated");
    public static readonly EventId TodoCompleted = new(1001, "TodoCompleted"); 
    public static readonly EventId TodoDeleted = new(1002, "TodoDeleted"); 
    public static readonly EventId TodoUpdated = new(1003, "TodoUpdated");

// エラーイベント
public static readonly EventId TodoOperationFailed = new(2000, "TodoOperationFailed");
public static readonly EventId TodoNotFound = new(2001, "TodoNotFound");
public static readonly EventId ValidationFailed = new(2002, "ValidationFailed");

// データベース操作イベント
public static readonly EventId DatabaseOperationFailed = new(3000, "DatabaseOperationFailed");
public static readonly EventId ConcurrencyConflict = new(3001, "ConcurrencyConflict");

// システムイベント
public static readonly EventId ApplicationStarted = new(9000, "ApplicationStarted");
public static readonly EventId DatabaseMigrated = new(9001, "DatabaseMigrated");

}

