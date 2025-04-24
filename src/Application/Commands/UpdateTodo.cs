using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using TodoApp.Application.Common;
using TodoApp.Application.DTOs;
using TodoApp.Domain.Repositories;

namespace TodoApp.Application.Commands;

public class UpdateTodo
{
    public record Command(Guid Id, UpdateTodoDto Data) : IRequest<Result<TodoDto>>;

    public class Handler : IRequestHandler<Command, Result<TodoDto>>
    {
        private readonly ITodoRepository _repository;

        public Handler(ITodoRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<TodoDto>> Handle(Command request, CancellationToken cancellationToken)
        {
            var todo = await _repository.GetByIdAsync(request.Id, cancellationToken);
            if (todo == null)
                return Result<TodoDto>.Failure($"Todo with ID {request.Id} was not found");

            todo.Update(
                request.Data.Title,
                request.Data.Description,
                request.Data.DueDate);

            if (request.Data.IsCompleted.HasValue)
            {
                if (request.Data.IsCompleted.Value)
                    todo.MarkAsComplete();
                else
                    todo.MarkAsIncomplete();
            }

            await _repository.UpdateAsync(todo, cancellationToken);

            var dto = new TodoDto
            {
                Id = todo.Id,
                Title = todo.Title,
                Description = todo.Description,
                IsCompleted = todo.IsCompleted,
                DueDate = todo.DueDate,
                CreatedAt = todo.CreatedAt,
                UpdatedAt = todo.UpdatedAt
            };

            return Result<TodoDto>.Success(dto);
        }
    }
}
