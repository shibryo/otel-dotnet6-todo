using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using TodoApp.Application.Common;
using TodoApp.Application.DTOs;
using TodoApp.Domain.Entities;
using TodoApp.Domain.Repositories;

namespace TodoApp.Application.Queries;

public class GetTodos
{
    public record Query(TodoFilterDto? Filter = null) : IRequest<Result<IReadOnlyList<TodoDto>>>;

    public class Handler : IRequestHandler<Query, Result<IReadOnlyList<TodoDto>>>
    {
        private readonly ITodoRepository _repository;

        public Handler(ITodoRepository repository)
        {
            _repository = repository;
        }

        private static TodoDto MapToDto(Todo todo)
        {
            return new TodoDto
            {
                Id = todo.Id,
                Title = todo.Title,
                Description = todo.Description,
                IsCompleted = todo.IsCompleted,
                DueDate = todo.DueDate,
                CreatedAt = todo.CreatedAt,
                UpdatedAt = todo.UpdatedAt
            };
        }

        public async Task<Result<IReadOnlyList<TodoDto>>> Handle(Query request, CancellationToken cancellationToken)
        {
            var filter = request.Filter;
            IReadOnlyList<Todo> todos;

            if (filter == null)
            {
                todos = await _repository.GetAllAsync(cancellationToken);
            }
            else
            {
                if (filter.IsCompleted.HasValue)
                {
                    todos = filter.IsCompleted.Value
                        ? await _repository.GetCompletedAsync(cancellationToken)
                        : await _repository.GetIncompleteAsync(cancellationToken);
                }
                else if (filter.DueBefore.HasValue)
                {
                    todos = await _repository.GetDueBeforeAsync(filter.DueBefore.Value, cancellationToken);
                }
                else if (filter.IsOverdue.HasValue && filter.IsOverdue.Value)
                {
                    todos = await _repository.GetOverdueAsync(cancellationToken);
                }
                else if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
                {
                    todos = await _repository.SearchByTitleAsync(filter.SearchTerm, cancellationToken);
                }
                else
                {
                    todos = await _repository.GetAllAsync(cancellationToken);
                }
            }

            var dtos = todos.Select(MapToDto).ToList().AsReadOnly();
            return Result<IReadOnlyList<TodoDto>>.Success(dtos);
        }
    }
}
