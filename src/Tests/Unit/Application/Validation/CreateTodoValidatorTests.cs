using System;
using TodoApp.Application.DTOs;
using TodoApp.Application.Validation;

namespace TodoApp.Tests.Unit.Application.Validation;

public class CreateTodoValidatorTests
{
    private readonly CreateTodoValidator _validator;

    public CreateTodoValidatorTests()
    {
        _validator = new CreateTodoValidator();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithEmptyTitle_ReturnsError(string title)
    {
        // Arrange
        var dto = new CreateTodoDto { Title = title };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(CreateTodoDto.Title) &&
            error.ErrorMessage == "Title is required");
    }

    [Fact]
    public void Validate_WithTooLongTitle_ReturnsError()
    {
        // Arrange
        var dto = new CreateTodoDto
        {
            Title = new string('a', 256)
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(CreateTodoDto.Title) &&
            error.ErrorMessage == "Title must not exceed 255 characters");
    }

    [Fact]
    public void Validate_WithPastDueDate_ReturnsError()
    {
        // Arrange
        var dto = new CreateTodoDto
        {
            Title = "Test Todo",
            DueDate = DateTimeOffset.UtcNow.AddDays(-1)
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(CreateTodoDto.DueDate) &&
            error.ErrorMessage == "Due date must be in the future");
    }

    [Fact]
    public void Validate_WithValidInput_ReturnsSuccess()
    {
        // Arrange
        var dto = new CreateTodoDto
        {
            Title = "Test Todo",
            Description = "Test Description",
            DueDate = DateTimeOffset.UtcNow.AddDays(1)
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }
}
