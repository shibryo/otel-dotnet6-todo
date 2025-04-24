using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using TodoApp.Application.Commands;
using TodoApp.Application.DTOs;
using TodoApp.Domain.Entities;
using TodoApp.Domain.Repositories;

namespace TodoApp.Tests.Unit.Application.Commands;

public class CreateTodoTests
{
    private readonly Mock<ITodoRepository> _mockRepository;
    private readonly CreateTodo.Handler _handler;

    public CreateTodoTests()
    {
        _mockRepository = new Mock<ITodoRepository>();
        _handler = new CreateTodo.Handler(_mockRepository);
    }

    [Fact]
    public async Task Handle_WithValidInput_ReturnsSuccessResult()
    {
        // Arrange
        var dto = new CreateTodoDto
        {
            Title = "Test Todo",
            Description = "Test Description",
            DueDate = DateTimeOffset.UtcNow.AddDays(1)
        };

        var command = new CreateTodo.Command(dto);

        Todo? savedTodo = null;
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<Todo>(), It.IsAny<CancellationToken>()))
            .Callback<Todo, CancellationToken>((todo, _) => savedTodo = todo)
            .ReturnsAsync((Todo todo, CancellationToken _) => todo);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(dto.Title, result.Value.Title);
        Assert.Equal(dto.Description, result.Value.Description);
        Assert.Equal(dto.DueDate, result.Value.DueDate);
        Assert.False(result.Value.IsCompleted);
        Assert.NotEqual(Guid.Empty, result.Value.Id);

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Todo>(), It.IsAny<CancellationToken>()), Times.Once);
        
        Assert.NotNull(savedTodo);
        Assert.Equal(dto.Title, savedTodo.Title);
        Assert.Equal(dto.Description, savedTodo.Description);
        Assert.Equal(dto.DueDate, savedTodo.DueDate);
        Assert.False(savedTodo.IsCompleted);
    }
}
