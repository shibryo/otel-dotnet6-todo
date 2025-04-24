using System;
using TodoApp.Domain.Exceptions;

namespace TodoApp.Domain.Entities;

public class Todo
{
    public Guid Id { get; private set; }
    public string Title { get; private set; }
    public string? Description { get; private set; }
    public bool IsCompleted { get; private set; }
    public DateTimeOffset? DueDate { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Todo()
    {
        // For EF Core
    }

    public Todo(string title, string? description = null, DateTimeOffset? dueDate = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new TodoDomainException("Title cannot be empty");

        if (title.Length > 255)
            throw new TodoDomainException("Title cannot be longer than 255 characters");

        Id = Guid.NewGuid();
        Title = title;
        Description = description;
        IsCompleted = false;
        DueDate = dueDate;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public void Update(string title, string? description = null, DateTimeOffset? dueDate = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new TodoDomainException("Title cannot be empty");

        if (title.Length > 255)
            throw new TodoDomainException("Title cannot be longer than 255 characters");

        Title = title;
        Description = description;
        DueDate = dueDate;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkAsComplete()
    {
        if (IsCompleted)
            return;

        IsCompleted = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkAsIncomplete()
    {
        if (!IsCompleted)
            return;

        IsCompleted = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
