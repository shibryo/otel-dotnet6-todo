using System;

namespace TodoApp.Application.DTOs;

public class UpdateTodoDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTimeOffset? DueDate { get; set; }
    public bool? IsCompleted { get; set; }
}
