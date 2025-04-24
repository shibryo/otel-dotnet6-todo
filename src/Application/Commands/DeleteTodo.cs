using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using TodoApp.Application.Common;
using TodoApp.Domain.Repositories;

namespace TodoApp.Application.Commands;

public class DeleteTodo
{
    public record Command(Guid Id) : IRequest<Result>;

    public class Handler : IRequestHandler<Command, Result>
    {
        private readonly ITodoRepository _repository;

        public Handler(ITodoRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            var todo = await _repository.GetByIdAsync(request.Id, cancellationToken);
            if (todo == null)
                return Result.Failure($"Todo with ID {request.Id} was not found");

            await _repository.DeleteAsync(todo, cancellationToken);
            return Result.Success();
        }
    }
}
