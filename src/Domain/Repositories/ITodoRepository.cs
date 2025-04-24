using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TodoApp.Domain.Entities;

namespace TodoApp.Domain.Repositories;

public interface ITodoRepository : IRepository<Todo>
{
    Task<IReadOnlyList<Todo>> GetCompletedAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Todo>> GetIncompleteAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Todo>> GetDueBeforeAsync(DateTimeOffset date, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Todo>> GetOverdueAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Todo>> SearchByTitleAsync(string searchTerm, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}
