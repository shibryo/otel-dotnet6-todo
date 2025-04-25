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
        using var activity = _activitySource.StartActivity("GetTodoItems");
        var items = await _context.TodoItems.ToListAsync();
        activity?.SetTag("todo.count", items.Count);
        return items;
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
        using var activity = _activitySource.StartActivity("CreateTodoItem");
        activity?.SetTag("todo.title", todoItem.Title);

        todoItem.CreatedAt = DateTime.UtcNow;
        _context.TodoItems.Add(todoItem);
        await _context.SaveChangesAsync();
        _metrics.TodoCreated();

        return CreatedAtAction(nameof(GetTodoItem), new { id = todoItem.Id }, todoItem);
    }

    // PUT: api/TodoItems/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutTodoItem(int id, TodoItem todoItem)
    {
        using var activity = _activitySource.StartActivity("UpdateTodoItem");
        activity?.SetTag("todo.id", id);
        activity?.SetTag("todo.title", todoItem.Title);
        activity?.SetTag("todo.is_complete", todoItem.IsComplete);

        if (id != todoItem.Id)
        {
            return BadRequest();
        }

        var existingTodoItem = await _context.TodoItems.FindAsync(id);
        if (existingTodoItem == null)
        {
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
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    // DELETE: api/TodoItems/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTodoItem(int id)
    {
        using var activity = _activitySource.StartActivity("DeleteTodoItem");
        activity?.SetTag("todo.id", id);

        var todoItem = await _context.TodoItems.FindAsync(id);
        if (todoItem == null)
        {
            return NotFound();
        }

        _metrics.TodoDeleted(todoItem.IsComplete);
        _context.TodoItems.Remove(todoItem);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool TodoItemExists(int id)
    {
        return _context.TodoItems.Any(e => e.Id == id);
    }
}
