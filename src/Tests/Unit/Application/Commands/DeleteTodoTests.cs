using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using TodoApp.Application.Commands;
using TodoApp.Domain.Entities;
using TodoApp.Domain.Repositories;

namespace TodoApp.Tests.Unit.Application.Commands;

public class DeleteTodoTests
{
    private readonly Mock<ITodoRepository> _mockRepository;
    private readonly DeleteTodo.Handler _handler;
    private readonly Todo _existingTodo;
    private readonly Guid _todoId;

    public DeleteTodoTests()
    {
        _mockRepository = new Mock<ITodoRepository>();
        _handler = new DeleteTodo.Handler(_mockRepository);

        _todoId = Guid.NewGuid();
        _existingTodo = new Todo("Test Todo");

        // リフレクションを使用してIdを設定
        typeof(Todo).GetProperty(nameof(Todo.Id))!
            .SetValue(_existingTodo, _todoId);
    }

    [Fact]
    public async Task Handle_WithExistingId_ReturnsSuccessResult()
    {
        // Arrange
        var command = new DeleteTodo.Command(_todoId);

        _mockRepository.Setup(r => r.GetByIdAsync(_todoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_existingTodo);

        Todo? deletedTodo = null;
        _mockRepository.Setup(r => r.DeleteAsync(It.IsAny<Todo>(), It.IsAny<CancellationToken>()))
            .Callback<Todo, CancellationToken>((todo, _) => deletedTodo = todo)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<Todo>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.NotNull(deletedTodo);
        Assert.Equal(_todoId, deletedTodo.Id);
    }

    [Fact]
    public async Task Handle_WithNonExistentId_ReturnsFailureResult()
    {
        // Arrange
        var command = new DeleteTodo.Command(_todoId);

        _mockRepository.Setup(r => r.GetByIdAsync(_todoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Todo?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal($"Todo with ID {_todoId} was not found", result.Error);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<Todo>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
