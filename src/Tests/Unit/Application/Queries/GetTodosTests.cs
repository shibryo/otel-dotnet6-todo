using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using TodoApp.Application.DTOs;
using TodoApp.Application.Queries;
using TodoApp.Domain.Entities;
using TodoApp.Domain.Repositories;

namespace TodoApp.Tests.Unit.Application.Queries;

public class GetTodosTests
{
    private readonly Mock<ITodoRepository> _mockRepository;
    private readonly GetTodos.Handler _handler;
    private readonly List<Todo> _testTodos;

    public GetTodosTests()
    {
        _mockRepository = new Mock<ITodoRepository>();
        _handler = new GetTodos.Handler(_mockRepository);

        _testTodos = new List<Todo>
        {
            CreateTodo("Task 1", false, DateTimeOffset.UtcNow.AddDays(1)),
            CreateTodo("Task 2", true, DateTimeOffset.UtcNow.AddDays(-1)),
            CreateTodo("Task 3", false, DateTimeOffset.UtcNow.AddDays(2))
        };
    }

    private static Todo CreateTodo(string title, bool isCompleted, DateTimeOffset? dueDate)
    {
        var todo = new Todo(title, description: null, dueDate);
        if (isCompleted)
            todo.MarkAsComplete();
        return todo;
    }

    [Fact]
    public async Task Handle_WithNoFilter_ReturnsAllTodos()
    {
        // Arrange
        var query = new GetTodos.Query();

        _mockRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testTodos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(_testTodos.Count, result.Value.Count);
        _mockRepository.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_WithIsCompletedFilter_ReturnsFilteredTodos(bool isCompleted)
    {
        // Arrange
        var filter = new TodoFilterDto { IsCompleted = isCompleted };
        var query = new GetTodos.Query(filter);

        var expectedTodos = _testTodos.Where(t => t.IsCompleted == isCompleted).ToList();
        
        if (isCompleted)
        {
            _mockRepository.Setup(r => r.GetCompletedAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedTodos);
        }
        else
        {
            _mockRepository.Setup(r => r.GetIncompleteAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedTodos);
        }

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.All(result.Value, dto => Assert.Equal(isCompleted, dto.IsCompleted));
        
        if (isCompleted)
        {
            _mockRepository.Verify(r => r.GetCompletedAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
        else
        {
            _mockRepository.Verify(r => r.GetIncompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    [Fact]
    public async Task Handle_WithDueBeforeFilter_ReturnsFilteredTodos()
    {
        // Arrange
        var dueDate = DateTimeOffset.UtcNow;
        var filter = new TodoFilterDto { DueBefore = dueDate };
        var query = new GetTodos.Query(filter);

        var expectedTodos = _testTodos.Where(t => t.DueDate < dueDate).ToList();
        _mockRepository.Setup(r => r.GetDueBeforeAsync(dueDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTodos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.All(result.Value, dto => Assert.True(dto.DueDate < dueDate));
        _mockRepository.Verify(r => r.GetDueBeforeAsync(dueDate, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithIsOverdueFilter_ReturnsFilteredTodos()
    {
        // Arrange
        var filter = new TodoFilterDto { IsOverdue = true };
        var query = new GetTodos.Query(filter);

        var expectedTodos = _testTodos.Where(t => t.DueDate < DateTimeOffset.UtcNow).ToList();
        _mockRepository.Setup(r => r.GetOverdueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTodos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        _mockRepository.Verify(r => r.GetOverdueAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithSearchTermFilter_ReturnsFilteredTodos()
    {
        // Arrange
        var searchTerm = "Task";
        var filter = new TodoFilterDto { SearchTerm = searchTerm };
        var query = new GetTodos.Query(filter);

        var expectedTodos = _testTodos.Where(t => t.Title.Contains(searchTerm)).ToList();
        _mockRepository.Setup(r => r.SearchByTitleAsync(searchTerm, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTodos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        _mockRepository.Verify(r => r.SearchByTitleAsync(searchTerm, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithEmptyFilter_ReturnsAllTodos()
    {
        // Arrange
        var filter = new TodoFilterDto();
        var query = new GetTodos.Query(filter);

        _mockRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testTodos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(_testTodos.Count, result.Value.Count);
        _mockRepository.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
