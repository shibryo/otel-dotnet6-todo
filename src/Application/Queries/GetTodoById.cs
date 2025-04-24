using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using TodoApp.Application.Common;
using TodoApp.Application.DTOs;
using TodoApp.Domain.Repositories;

namespace TodoApp.Application.Queries;

public class GetTodoById
{
    public record Query(Guid Id) : IRequest<Result<TodoDto>>;

    public class Handler : IRequestHandler<Query, Result<TodoDto>>
    {
        private readonly ITodoRepository _repository;

        public Handler(ITodoRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<TodoDto>> Handle(Query request, CancellationToken cancellationToken)
        {
            var todo = await _repository.GetByIdAsync(request.Id, cancellationToken);
            if (todo == null)
                return Result<TodoDto>.Failure($"Todo with ID {request.Id} was not found");

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
