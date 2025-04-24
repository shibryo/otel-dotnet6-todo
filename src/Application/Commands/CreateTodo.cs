using System.Threading;
using System.Threading.Tasks;
using MediatR;
using TodoApp.Application.Common;
using TodoApp.Application.DTOs;
using TodoApp.Domain.Entities;
using TodoApp.Domain.Repositories;

namespace TodoApp.Application.Commands;

public class CreateTodo
{
    public record Command(CreateTodoDto Data) : IRequest<Result<TodoDto>>;

    public class Handler : IRequestHandler<Command, Result<TodoDto>>
    {
        private readonly ITodoRepository _repository;

        public Handler(ITodoRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<TodoDto>> Handle(Command request, CancellationToken cancellationToken)
        {
            var todo = new Todo(
                request.Data.Title,
                request.Data.Description,
                request.Data.DueDate);

            var created = await _repository.AddAsync(todo, cancellationToken);

            var dto = new TodoDto
            {
                Id = created.Id,
                Title = created.Title,
                Description = created.Description,
                IsCompleted = created.IsCompleted,
                DueDate = created.DueDate,
                CreatedAt = created.CreatedAt,
                UpdatedAt = created.UpdatedAt
            };

            return Result<TodoDto>.Success(dto);
        }
    }
}
