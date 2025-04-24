using System;

namespace TodoApp.Application.DTOs;

public class TodoFilterDto
{
    public bool? IsCompleted { get; set; }
    public DateTimeOffset? DueBefore { get; set; }
    public bool? IsOverdue { get; set; }
    public string? SearchTerm { get; set; }
}
