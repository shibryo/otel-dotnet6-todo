using System.Diagnostics;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using TodoApp.Api.Extensions;
using TodoApp.Application.Commands;
using TodoApp.Application.DTOs;
using TodoApp.Application.Queries;

namespace TodoApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TodoController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ActivitySource _activitySource;

    public TodoController(IMediator mediator, ActivitySource activitySource)
    {
        _mediator = mediator;
        _activitySource = activitySource;
    }

    [HttpPost]
    public async Task<ActionResult<TodoDto>> Create([FromBody] CreateTodoDto createTodoDto, CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity("CreateTodo");
        activity?.SetTag("todo.title", createTodoDto.Title);
        
        var startTime = DateTime.UtcNow;
        var command = new CreateTodo(createTodoDto);
        var result = await _mediator.Send(command, cancellationToken);
        
        TelemetryConstants.RequestDuration.Record((DateTime.UtcNow - startTime).TotalMilliseconds);
        
        if (result.IsSuccess)
        {
            activity?.SetTag("todo.id", result.Value.Id);
            TelemetryConstants.TodoItemsCreated.Add(1);
            return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
        }

        return BadRequest(result.Error);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<TodoDto>> Update(Guid id, [FromBody] UpdateTodoDto updateTodoDto, CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity("UpdateTodo");
        activity?.SetTag("todo.id", id);
        activity?.SetTag("todo.completed", updateTodoDto.IsCompleted);
        
        var startTime = DateTime.UtcNow;
        var command = new UpdateTodo(id, updateTodoDto);
        var result = await _mediator.Send(command, cancellationToken);
        
        TelemetryConstants.RequestDuration.Record((DateTime.UtcNow - startTime).TotalMilliseconds);

        if (result.IsSuccess)
        {
            if (result.Value.IsCompleted)
            {
                TelemetryConstants.TodoItemsCompleted.Add(1);
            }
            return Ok(result.Value);
        }

        return NotFound(result.Error);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity("DeleteTodo");
        activity?.SetTag("todo.id", id);
        
        var startTime = DateTime.UtcNow;
        var command = new DeleteTodo(id);
        var result = await _mediator.Send(command, cancellationToken);
        
        TelemetryConstants.RequestDuration.Record((DateTime.UtcNow - startTime).TotalMilliseconds);

        if (result.IsSuccess)
        {
            return NoContent();
        }

        return NotFound(result.Error);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TodoDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity("GetTodoById");
        activity?.SetTag("todo.id", id);
        
        var startTime = DateTime.UtcNow;
        var query = new GetTodoById(id);
        var result = await _mediator.Send(query, cancellationToken);
        
        TelemetryConstants.RequestDuration.Record((DateTime.UtcNow - startTime).TotalMilliseconds);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return NotFound(result.Error);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TodoDto>>> GetList([FromQuery] TodoFilterDto filter, CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity("GetTodoList");
        activity?.SetTag("filter.completed", filter.IsCompleted);
        
        var startTime = DateTime.UtcNow;
        var query = new GetTodos(filter);
        var result = await _mediator.Send(query, cancellationToken);
        
        TelemetryConstants.RequestDuration.Record((DateTime.UtcNow - startTime).TotalMilliseconds);

        return Ok(result.Value);
    }
}
