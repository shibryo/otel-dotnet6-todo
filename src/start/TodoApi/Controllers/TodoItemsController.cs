using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Models;
using TodoApi.Metrics;
using TodoApi.Logging;
using System.Diagnostics;

namespace TodoApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TodoItemsController : ControllerBase
{
    private readonly TodoContext _context;
    private readonly TodoMetrics _metrics;
    private readonly ILogger<TodoItemsController> _logger;
    private static readonly ActivitySource _activitySource = new("TodoApi");


    public TodoItemsController(TodoContext context, TodoMetrics metrics, ILogger<TodoItemsController> logger)
    {
        _context = context;
        _metrics = metrics;
        _logger = logger;
    }

    // GET: api/TodoItems
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TodoItem>>> GetTodoItems()
    {
        using var activity = _activitySource.StartActivity("GetAllTodos");
        
        var todos = await _context.TodoItems.ToListAsync();
        activity?.SetTag("todo.count", todos.Count);
        activity?.SetTag("todo.completed_count", todos.Count(t => t.IsComplete));
        
        return todos;
    }

    // GET: api/TodoItems/5
    [HttpGet("{id}")]
    public async Task<ActionResult<TodoItem>> GetTodoItem(int id)
    {
        using var activity = _activitySource.StartActivity("GetTodoById");
        activity?.SetTag("todo.id", id);

        var todoItem = await _context.TodoItems.FindAsync(id);

        if (todoItem == null)
        {
            activity?.SetTag("error", "Todo not found");
            activity?.SetTag("error.details", $"Todo with id {id} does not exist");

            return NotFound();
        }

        activity?.SetTag("todo.state", $"Title: {todoItem.Title}, IsComplete: {todoItem.IsComplete}");
        if (todoItem.CompletedAt.HasValue)
        {
            activity?.SetTag("todo.completed_at", todoItem.CompletedAt);
        }

        return todoItem;
    }

    // POST: api/TodoItems
    [HttpPost]
    public async Task<ActionResult<TodoItem>> PostTodoItem(TodoItem todoItem)
    {
        using var activity = _activitySource.StartActivity("CreateTodo");
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["TraceId"] = activity?.TraceId.ToString() ?? "unknown",
            ["SpanId"] = activity?.SpanId.ToString() ?? "unknown"
        }))
        {
            try
            {
                _logger.LogInformation(LogEvents.TodoCreated, 
                    "Creating todo item: {TodoTitle}", todoItem.Title);

                _metrics.TodoCreated();
                activity?.SetTag("todo.title", todoItem.Title);
                
                todoItem.CreatedAt = DateTime.UtcNow;
                _context.TodoItems.Add(todoItem);
                await _context.SaveChangesAsync();

                activity?.SetTag("todo.id", todoItem.Id);
                _logger.LogInformation(LogEvents.TodoCreated, 
                    "Todo item created successfully: {TodoId}", todoItem.Id);

                return CreatedAtAction(nameof(GetTodoItem), new { id = todoItem.Id }, todoItem);
            }
            catch (Exception ex)
            {
                _logger.LogError(LogEvents.TodoOperationFailed, ex, 
                    "Failed to create todo item: {TodoTitle}", todoItem.Title);
                throw;
            }
        }
    }

    // PUT: api/TodoItems/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutTodoItem(int id, TodoItem todoItem)
    {
        using var activity = _activitySource.StartActivity("UpdateTodo");
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["TraceId"] = activity?.TraceId.ToString() ?? "unknown",
            ["SpanId"] = activity?.SpanId.ToString() ?? "unknown",
            ["TodoId"] = id
        }))
        {
            try
            {
                activity?.SetTag("todo.id", id);
                
                if (id != todoItem.Id)
                {
                    activity?.SetTag("error", "ID mismatch");
                    _logger.LogWarning(LogEvents.ValidationFailed, 
                        "Todo update failed: ID mismatch. Path ID: {PathId}, Body ID: {BodyId}", 
                        id, todoItem.Id);
                    return BadRequest();
                }

                var existingTodoItem = await _context.TodoItems.FindAsync(id);
                if (existingTodoItem == null)
                {
                    activity?.SetTag("error", "Todo not found");
                    _logger.LogWarning(LogEvents.TodoNotFound, 
                        "Todo item not found: {TodoId}", id);
                    return NotFound();
                }

                _logger.LogInformation(LogEvents.TodoUpdated, 
                    "Updating todo item: {TodoId}, Original state: {OriginalState}",
                    id, $"Title: {existingTodoItem.Title}, IsComplete: {existingTodoItem.IsComplete}");
                activity?.SetTag("todo.original_state", $"Title: {existingTodoItem.Title}, IsComplete: {existingTodoItem.IsComplete}");
                
                existingTodoItem.Title = todoItem.Title;
                existingTodoItem.IsComplete = todoItem.IsComplete;
                if (todoItem.IsComplete && !existingTodoItem.CompletedAt.HasValue)
                {
                    existingTodoItem.CompletedAt = DateTime.UtcNow;
                    activity?.SetTag("todo.completed", true);
                    activity?.SetTag("todo.completed_at", existingTodoItem.CompletedAt);
                    _metrics.TodoCompleted();
                    _logger.LogInformation(LogEvents.TodoCompleted, 
                        "Todo item completed: {TodoId} at {CompletedAt}",
                        id, existingTodoItem.CompletedAt);
                }
                else if (!todoItem.IsComplete)
                {
                    existingTodoItem.CompletedAt = null;
                    activity?.SetTag("todo.completed", false);
                    _logger.LogInformation(LogEvents.TodoUpdated, 
                        "Todo item marked as incomplete: {TodoId}", id);
                }

                try
                {
                    await _context.SaveChangesAsync();
                    activity?.SetTag("todo.new_state", $"Title: {existingTodoItem.Title}, IsComplete: {existingTodoItem.IsComplete}");
                    _logger.LogInformation(LogEvents.TodoUpdated, 
                        "Todo item updated successfully: {TodoId}, New state: {NewState}",
                        id, $"Title: {existingTodoItem.Title}, IsComplete: {existingTodoItem.IsComplete}");
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    if (!TodoItemExists(id))
                    {
                        activity?.SetTag("error", "Concurrency error - Todo not found");
                        _logger.LogWarning(LogEvents.ConcurrencyConflict, 
                            "Concurrency error - Todo not found: {TodoId}", id);
                        return NotFound();
                    }
                    else
                    {
                        activity?.SetTag("error", "Concurrency error");
                        _logger.LogError(LogEvents.DatabaseOperationFailed, ex,
                            "Failed to update todo item due to concurrency error: {TodoId}", id);
                        throw;
                    }
                }

                return NoContent();
            } catch (Exception ex)
            {
                _logger.LogError(LogEvents.TodoOperationFailed, ex,
                    "Failed to update todo item: {TodoId}", id);
                throw;
            }
        }
    }

    // DELETE: api/TodoItems/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTodoItem(int id)
    {
        using var activity = _activitySource.StartActivity("DeleteTodo");
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["TraceId"] = activity?.TraceId.ToString() ?? "unknown",
            ["SpanId"] = activity?.SpanId.ToString() ?? "unknown",
            ["TodoId"] = id
        }))
        {
            try
            {
                activity?.SetTag("todo.id", id);
                _logger.LogInformation(LogEvents.TodoDeleted,
                    "Attempting to delete todo item: {TodoId}", id);

                var todoItem = await _context.TodoItems.FindAsync(id);
                if (todoItem == null)
                {
                    activity?.SetTag("error", "Todo not found");
                    _logger.LogWarning(LogEvents.TodoNotFound,
                        "Todo item not found: {TodoId}", id);
                    return NotFound();
                }

                activity?.SetTag("todo.state", $"Title: {todoItem.Title}, IsComplete: {todoItem.IsComplete}");
                _logger.LogInformation(LogEvents.TodoDeleted,
                    "Deleting todo item: {TodoId}, State: {State}",
                    id, $"Title: {todoItem.Title}, IsComplete: {todoItem.IsComplete}");
                
                _context.TodoItems.Remove(todoItem);
                await _context.SaveChangesAsync();

                activity?.SetTag("todo.deleted", true);
                _logger.LogInformation(LogEvents.TodoDeleted,
                    "Todo item deleted successfully: {TodoId}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(LogEvents.TodoOperationFailed, ex,
                    "Failed to delete todo item: {TodoId}", id);
                throw;
            }
        }
    }

    private bool TodoItemExists(int id)
    {
        return _context.TodoItems.Any(e => e.Id == id);
    }
}
