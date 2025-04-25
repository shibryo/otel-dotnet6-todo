using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using TodoApi.Data;
using TodoApi.Models;
using TodoApi.Metrics;

namespace TodoApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TodoItemsController : ControllerBase
{
    private readonly TodoContext _context;
    private readonly TodoMetrics _metrics;
    private static readonly ActivitySource _activitySource = new("TodoApi");

    public TodoItemsController(TodoContext context, TodoMetrics metrics)
    {
        _context = context;
        _metrics = metrics;
    }

    // GET: api/TodoItems
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TodoItem>>> GetTodoItems()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var activity = _activitySource.StartActivity("GetTodoItems");
            var items = await _context.TodoItems.ToListAsync();
            activity?.SetTag("todo.count", items.Count);
            return items;
        }
        catch (Exception ex)
        {
            _metrics.RecordOperationError("get_all", ex.GetType().Name);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _metrics.RecordApiResponseTime(stopwatch.Elapsed.TotalMilliseconds, "get_all");
        }
    }

    // GET: api/TodoItems/5
    [HttpGet("{id}")]
    public async Task<ActionResult<TodoItem>> GetTodoItem(int id)
    {
        using var activity = _activitySource.StartActivity("GetTodoItem");
        activity?.SetTag("todo.id", id);

        var todoItem = await _context.TodoItems.FindAsync(id);

        if (todoItem == null)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Todo item not found");
            _metrics.RecordOperationError("get_by_id", "NotFound");
            return NotFound();
        }

        activity?.SetTag("todo.title", todoItem.Title);
        activity?.SetTag("todo.is_complete", todoItem.IsComplete);
        return todoItem;
    }

    // POST: api/TodoItems
    [HttpPost]
    public async Task<ActionResult<TodoItem>> PostTodoItem(TodoItem todoItem)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var activity = _activitySource.StartActivity("CreateTodoItem");
            activity?.SetTag("todo.title", todoItem.Title);

            todoItem.CreatedAt = DateTime.UtcNow;
            _context.TodoItems.Add(todoItem);
            await _context.SaveChangesAsync();
            
            var priority = todoItem.Title.ToLower().Contains("urgent") ? "high" : "normal";
            _metrics.TodoCreated(priority);

            return CreatedAtAction(nameof(GetTodoItem), new { id = todoItem.Id }, todoItem);
        }
        catch (Exception ex)
        {
            _metrics.RecordOperationError("create", ex.GetType().Name);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _metrics.RecordApiResponseTime(stopwatch.Elapsed.TotalMilliseconds, "create");
        }
    }

    // PUT: api/TodoItems/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutTodoItem(int id, TodoItem todoItem)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var activity = _activitySource.StartActivity("UpdateTodoItem");
            activity?.SetTag("todo.id", id);
            activity?.SetTag("todo.title", todoItem.Title);
            activity?.SetTag("todo.is_complete", todoItem.IsComplete);

            if (id != todoItem.Id)
            {
                _metrics.RecordOperationError("update", "BadRequest");
                return BadRequest();
            }

            var existingTodoItem = await _context.TodoItems.FindAsync(id);
            if (existingTodoItem == null)
            {
                _metrics.RecordOperationError("update", "NotFound");
                return NotFound();
            }

            existingTodoItem.Title = todoItem.Title;
            existingTodoItem.IsComplete = todoItem.IsComplete;
            if (todoItem.IsComplete && !existingTodoItem.CompletedAt.HasValue)
            {
                existingTodoItem.CompletedAt = DateTime.UtcNow;
                _metrics.TodoCompleted(existingTodoItem.CreatedAt);
            }
            else if (!todoItem.IsComplete)
            {
                existingTodoItem.CompletedAt = null;
                _metrics.TodoUncompleted();
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TodoItemExists(id))
                {
                    _metrics.RecordOperationError("update", "ConcurrencyNotFound");
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _metrics.RecordOperationError("update", ex.GetType().Name);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _metrics.RecordApiResponseTime(stopwatch.Elapsed.TotalMilliseconds, "update");
        }
    }

    // DELETE: api/TodoItems/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTodoItem(int id)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var activity = _activitySource.StartActivity("DeleteTodoItem");
            activity?.SetTag("todo.id", id);

            var todoItem = await _context.TodoItems.FindAsync(id);
            if (todoItem == null)
            {
                _metrics.RecordOperationError("delete", "NotFound");
                return NotFound();
            }

            var priority = todoItem.Title.ToLower().Contains("urgent") ? "high" : "normal";
            _metrics.TodoDeleted(todoItem.IsComplete, priority);
            _context.TodoItems.Remove(todoItem);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _metrics.RecordOperationError("delete", ex.GetType().Name);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _metrics.RecordApiResponseTime(stopwatch.Elapsed.TotalMilliseconds, "delete");
        }
    }

    private bool TodoItemExists(int id)
    {
        return _context.TodoItems.Any(e => e.Id == id);
    }
}
