using MediatR;
using Microsoft.AspNetCore.Mvc;
using TodoApp.Application.Commands;
using TodoApp.Application.DTOs;
using TodoApp.Application.Queries;

namespace TodoApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TodoController : ControllerBase
{
    private readonly IMediator _mediator;

    public TodoController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<ActionResult<TodoDto>> Create([FromBody] CreateTodoDto createTodoDto, CancellationToken cancellationToken)
    {
        var command = new CreateTodo(createTodoDto);
        var result = await _mediator.Send(command, cancellationToken);
        
        if (result.IsSuccess)
        {
            return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
        }

        return BadRequest(result.Error);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<TodoDto>> Update(Guid id, [FromBody] UpdateTodoDto updateTodoDto, CancellationToken cancellationToken)
    {
        var command = new UpdateTodo(id, updateTodoDto);
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return NotFound(result.Error);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var command = new DeleteTodo(id);
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            return NoContent();
        }

        return NotFound(result.Error);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TodoDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetTodoById(id);
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return NotFound(result.Error);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TodoDto>>> GetList([FromQuery] TodoFilterDto filter, CancellationToken cancellationToken)
    {
        var query = new GetTodos(filter);
        var result = await _mediator.Send(query, cancellationToken);

        return Ok(result.Value);
    }
}
