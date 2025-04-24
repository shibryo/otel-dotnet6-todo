using FluentValidation;
using TodoApp.Application.DTOs;

namespace TodoApp.Application.Validation;

public class UpdateTodoValidator : AbstractValidator<UpdateTodoDto>
{
    public UpdateTodoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(255).WithMessage("Title must not exceed 255 characters");

        RuleFor(x => x.DueDate)
            .Must(date => !date.HasValue || date.Value > DateTimeOffset.UtcNow)
            .WithMessage("Due date must be in the future");

        RuleFor(x => x.IsCompleted)
            .NotNull().WithMessage("IsCompleted status must be specified");
    }
}
