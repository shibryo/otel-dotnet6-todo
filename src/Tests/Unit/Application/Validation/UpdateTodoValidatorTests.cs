using System;
using TodoApp.Application.DTOs;
using TodoApp.Application.Validation;

namespace TodoApp.Tests.Unit.Application.Validation;

public class UpdateTodoValidatorTests
{
    private readonly UpdateTodoValidator _validator;

    public UpdateTodoValidatorTests()
    {
        _validator = new UpdateTodoValidator();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithEmptyTitle_ReturnsError(string title)
    {
        // Arrange
        var dto = new UpdateTodoDto
        {
            Title = title,
            IsCompleted = true
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(UpdateTodoDto.Title) &&
            error.ErrorMessage == "Title is required");
    }

    [Fact]
    public void Validate_WithTooLongTitle_ReturnsError()
    {
        // Arrange
        var dto = new UpdateTodoDto
        {
            Title = new string('a', 256),
            IsCompleted = true
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(UpdateTodoDto.Title) &&
            error.ErrorMessage == "Title must not exceed 255 characters");
    }

    [Fact]
    public void Validate_WithPastDueDate_ReturnsError()
    {
        // Arrange
        var dto = new UpdateTodoDto
        {
            Title = "Test Todo",
            DueDate = DateTimeOffset.UtcNow.AddDays(-1),
            IsCompleted = true
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(UpdateTodoDto.DueDate) &&
            error.ErrorMessage == "Due date must be in the future");
    }

    [Fact]
    public void Validate_WithoutIsCompleted_ReturnsError()
    {
        // Arrange
        var dto = new UpdateTodoDto
        {
            Title = "Test Todo",
            DueDate = DateTimeOffset.UtcNow.AddDays(1)
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(UpdateTodoDto.IsCompleted) &&
            error.ErrorMessage == "IsCompleted status must be specified");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Validate_WithValidInput_ReturnsSuccess(bool isCompleted)
    {
        // Arrange
        var dto = new UpdateTodoDto
        {
            Title = "Test Todo",
            Description = "Test Description",
            DueDate = DateTimeOffset.UtcNow.AddDays(1),
            IsCompleted = isCompleted
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }
}
