using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using TodoApp.Application.Queries;
using TodoApp.Domain.Entities;
using TodoApp.Domain.Repositories;

namespace TodoApp.Tests.Unit.Application.Queries;

public class GetTodoByIdTests
{
    private readonly Mock<ITodoRepository> _mockRepository;
    private readonly GetTodoById.Handler _handler;
    private readonly Todo _existingTodo;
    private readonly Guid _todoId;

    public GetTodoByIdTests()
    {
        _mockRepository = new Mock<ITodoRepository>();
        _handler = new GetTodoById.Handler(_mockRepository);

        _todoId = Guid.NewGuid();
        _existingTodo = new Todo(
            "Test Todo",
            "Test Description",
            DateTimeOffset.UtcNow.AddDays(1));

        // リフレクションを使用してIdを設定
        typeof(Todo).GetProperty(nameof(Todo.Id))!
            .SetValue(_existingTodo, _todoId);
    }

    [Fact]
    public async Task Handle_WithExistingId_ReturnsSuccessResult()
    {
        // Arrange
        var query = new GetTodoById.Query(_todoId);

        _mockRepository.Setup(r => r.GetByIdAsync(_todoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_existingTodo);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(_todoId, result.Value.Id);
        Assert.Equal(_existingTodo.Title, result.Value.Title);
        Assert.Equal(_existingTodo.Description, result.Value.Description);
        Assert.Equal(_existingTodo.IsCompleted, result.Value.IsCompleted);
        Assert.Equal(_existingTodo.DueDate, result.Value.DueDate);
        Assert.Equal(_existingTodo.CreatedAt, result.Value.CreatedAt);
        Assert.Equal(_existingTodo.UpdatedAt, result.Value.UpdatedAt);
    }

    [Fact]
    public async Task Handle_WithNonExistentId_ReturnsFailureResult()
    {
        // Arrange
        var query = new GetTodoById.Query(_todoId);

        _mockRepository.Setup(r => r.GetByIdAsync(_todoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Todo?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal($"Todo with ID {_todoId} was not found", result.Error);
    }
}
