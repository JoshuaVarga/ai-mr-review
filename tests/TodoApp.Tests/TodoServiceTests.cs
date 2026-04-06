using TodoApp.Core.Interfaces;
using TodoApp.Core.Models;
using TodoApp.Services;

namespace TodoApp.Tests;

/// <summary>
/// Simple in-memory repository for unit testing TodoService in isolation.
/// </summary>
file class InMemoryTodoRepository : ITodoRepository
{
    private readonly List<TodoItem> _items = [];

    public IReadOnlyList<TodoItem> LoadAll() => [.. _items];
    public void Add(TodoItem item) => _items.Add(item);
    public void Update(TodoItem item)
    {
        var i = _items.FindIndex(t => t.Id == item.Id);
        if (i >= 0) _items[i] = item;
    }
    public void Delete(Guid id) => _items.RemoveAll(t => t.Id == id);
}

public class TodoServiceTests
{
    private readonly TodoService _service = new(new InMemoryTodoRepository());

    private IReadOnlyList<TodoItem> GetCurrentTodos()
    {
        IReadOnlyList<TodoItem> result = [];
        using var sub = _service.Todos.Subscribe(t => result = t);
        return result;
    }

    [Fact]
    public void Add_CreatesNewTodo()
    {
        _service.Add("Buy milk");

        var todos = GetCurrentTodos();
        Assert.Single(todos);
        Assert.Equal("Buy milk", todos[0].Title);
        Assert.False(todos[0].IsCompleted);
    }

    [Fact]
    public void Add_TrimsWhitespace()
    {
        _service.Add("  Buy milk  ");

        var todos = GetCurrentTodos();
        Assert.Equal("Buy milk", todos[0].Title);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Add_IgnoresEmptyOrWhitespace(string? title)
    {
        _service.Add(title!);

        var todos = GetCurrentTodos();
        Assert.Empty(todos);
    }

    [Fact]
    public void Toggle_CompletesAndUncompletesTodo()
    {
        _service.Add("Buy milk");
        var id = GetCurrentTodos()[0].Id;

        _service.Toggle(id);
        Assert.True(GetCurrentTodos()[0].IsCompleted);

        _service.Toggle(id);
        Assert.False(GetCurrentTodos()[0].IsCompleted);
    }

    [Fact]
    public void Toggle_IgnoresUnknownId()
    {
        _service.Add("Buy milk");

        _service.Toggle(Guid.NewGuid());

        var todos = GetCurrentTodos();
        Assert.Single(todos);
        Assert.False(todos[0].IsCompleted);
    }

    [Fact]
    public void Remove_DeletesTodo()
    {
        _service.Add("Buy milk");
        _service.Add("Walk dog");
        var id = GetCurrentTodos()[0].Id;

        _service.Remove(id);

        var todos = GetCurrentTodos();
        Assert.Single(todos);
        Assert.Equal("Walk dog", todos[0].Title);
    }

    [Fact]
    public void Remove_IgnoresUnknownId()
    {
        _service.Add("Buy milk");

        _service.Remove(Guid.NewGuid());

        Assert.Single(GetCurrentTodos());
    }
}
