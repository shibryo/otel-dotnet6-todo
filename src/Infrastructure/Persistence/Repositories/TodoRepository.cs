using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TodoApp.Domain.Entities;
using TodoApp.Domain.Repositories;

namespace TodoApp.Infrastructure.Persistence.Repositories;

public class TodoRepository : ITodoRepository
{
    private readonly TodoDbContext _context;

    public TodoRepository(TodoDbContext context)
    {
        _context = context;
    }

    public async Task<Todo?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Todos.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<IReadOnlyList<Todo>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Todos.ToListAsync(cancellationToken);
    }

    public async Task<Todo> AddAsync(Todo entity, CancellationToken cancellationToken = default)
    {
        await _context.Todos.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task UpdateAsync(Todo entity, CancellationToken cancellationToken = default)
    {
        _context.Entry(entity).State = EntityState.Modified;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Todo entity, CancellationToken cancellationToken = default)
    {
        _context.Todos.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Todo>> GetCompletedAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Todos
            .Where(t => t.IsCompleted)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Todo>> GetIncompleteAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Todos
            .Where(t => !t.IsCompleted)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Todo>> GetDueBeforeAsync(DateTimeOffset date, CancellationToken cancellationToken = default)
    {
        return await _context.Todos
            .Where(t => t.DueDate != null && t.DueDate < date)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Todo>> GetOverdueAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        return await _context.Todos
            .Where(t => !t.IsCompleted && t.DueDate != null && t.DueDate < now)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Todo>> SearchByTitleAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        return await _context.Todos
            .Where(t => EF.Functions.ILike(t.Title, $"%{searchTerm}%"))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Todos.AnyAsync(t => t.Id == id, cancellationToken);
    }
}
