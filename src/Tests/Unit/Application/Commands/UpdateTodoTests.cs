using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using TodoApp.Application.Commands;
using TodoApp.Application.DTOs;
using TodoApp.Domain.Entities;
using TodoApp.Domain.Repositories;

namespace TodoApp.Tests.Unit.Application.Commands;

public class UpdateTodoTests
{
    private readonly Mock<ITodoRepository> _mockRepository;
    private readonly UpdateTodo.Handler _handler;
    private readonly Todo _existingTodo;
    private readonly Guid _todoId;

    public UpdateTodoTests()
    {
        _mockRepository = new Mock<ITodoRepository>();
        _handler = new UpdateTodo.Handler(_mockRepository);
        
        _todoId = Guid.NewGuid();
        _existingTodo = new Todo("Original Title", "Original Description");
        
        // リフレクションを使用してIdを設定
        typeof(Todo).GetProperty(nameof(Todo.Id))!
            .SetValue(_existingTodo, _todoId);
    }

    [Fact]
    public async Task Handle_WithValidInput_ReturnsSuccessResult()
    {
        // Arrange
        var dto = new UpdateTodoDto
        {
            Title = "Updated Title",
            Description = "Updated Description",
            DueDate = DateTimeOffset.UtcNow.AddDays(1)
        };

        var command = new UpdateTodo.Command(_todoId, dto);

        _mockRepository.Setup(r => r.GetByIdAsync(_todoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_existingTodo);

        Todo? updatedTodo = null;
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Todo>(), It.IsAny<CancellationToken>()))
            .Callback<Todo, CancellationToken>((todo, _) => updatedTodo = todo)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(dto.Title, result.Value.Title);
        Assert.Equal(dto.Description, result.Value.Description);
        Assert.Equal(dto.DueDate, result.Value.DueDate);
        Assert.False(result.Value.IsCompleted);

        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Todo>(), It.IsAny<CancellationToken>()), Times.Once);
        
        Assert.NotNull(updatedTodo);
        Assert.Equal(dto.Title, updatedTodo.Title);
        Assert.Equal(dto.Description, updatedTodo.Description);
        Assert.Equal(dto.DueDate, updatedTodo.DueDate);
    }

    [Fact]
    public async Task Handle_WithNonExistentId_ReturnsFailureResult()
    {
        // Arrange
        var dto = new UpdateTodoDto
        {
            Title = "Updated Title"
        };

        var command = new UpdateTodo.Command(_todoId, dto);

        _mockRepository.Setup(r => r.GetByIdAsync(_todoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Todo?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal($"Todo with ID {_todoId} was not found", result.Error);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Todo>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_WithIsCompleted_UpdatesCompletionStatus(bool isCompleted)
    {
        // Arrange
        var dto = new UpdateTodoDto
        {
            Title = "Updated Title",
            IsCompleted = isCompleted
        };

        var command = new UpdateTodo.Command(_todoId, dto);

        _mockRepository.Setup(r => r.GetByIdAsync(_todoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_existingTodo);

        Todo? updatedTodo = null;
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Todo>(), It.IsAny<CancellationToken>()))
            .Callback<Todo, CancellationToken>((todo, _) => updatedTodo = todo)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(isCompleted, result.Value.IsCompleted);

        Assert.NotNull(updatedTodo);
        Assert.Equal(isCompleted, updatedTodo.IsCompleted);
    }
}
