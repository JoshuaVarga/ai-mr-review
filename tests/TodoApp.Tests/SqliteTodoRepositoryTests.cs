using TodoApp.Core.Models;
using TodoApp.DAL;

namespace TodoApp.Tests;

public class SqliteTodoRepositoryTests : IDisposable
{
    // SQLite shared-cache in-memory DB with a named URI so the same DB
    // is reused across multiple connections within the same test.
    private readonly string _dbPath = $"file:test-{Guid.NewGuid():N}?mode=memory&cache=shared";
    private readonly SqliteTodoRepository _repo;

    public SqliteTodoRepositoryTests()
    {
        _repo = new SqliteTodoRepository(_dbPath);
    }

    public void Dispose() { }

    [Fact]
    public void LoadAll_EmptyOnStart()
    {
        Assert.Empty(_repo.LoadAll());
    }

    [Fact]
    public void Add_AndLoadAll_ReturnsItem()
    {
        var item = new TodoItem(Guid.NewGuid(), "Buy milk", false);
        _repo.Add(item);

        var items = _repo.LoadAll();
        Assert.Single(items);
        Assert.Equal(item.Id, items[0].Id);
        Assert.Equal("Buy milk", items[0].Title);
        Assert.False(items[0].IsCompleted);
    }

    [Fact]
    public void Update_ChangesIsCompleted()
    {
        var item = new TodoItem(Guid.NewGuid(), "Buy milk", false);
        _repo.Add(item);

        _repo.Update(item with { IsCompleted = true });

        Assert.True(_repo.LoadAll()[0].IsCompleted);
    }

    [Fact]
    public void Delete_RemovesItem()
    {
        var item = new TodoItem(Guid.NewGuid(), "Buy milk", false);
        _repo.Add(item);

        _repo.Delete(item.Id);

        Assert.Empty(_repo.LoadAll());
    }

    [Fact]
    public void LoadAll_PreservesInsertionOrder()
    {
        _repo.Add(new TodoItem(Guid.NewGuid(), "First", false));
        _repo.Add(new TodoItem(Guid.NewGuid(), "Second", false));
        _repo.Add(new TodoItem(Guid.NewGuid(), "Third", false));

        var items = _repo.LoadAll();
        Assert.Equal(["First", "Second", "Third"], items.Select(t => t.Title));
    }
}
