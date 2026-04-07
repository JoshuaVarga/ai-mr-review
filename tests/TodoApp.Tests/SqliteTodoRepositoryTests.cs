using TodoApp.Core.Models;
using TodoApp.DAL;

namespace TodoApp.Tests;

public class SqliteTodoRepositoryTests : IDisposable
{
    private readonly string _dbPath = $"file:test-{Guid.NewGuid():N}?mode=memory&cache=shared";
    private readonly SqliteTodoRepository _repo;

    public SqliteTodoRepositoryTests()
    {
        _repo = new SqliteTodoRepository(_dbPath);
    }

    public void Dispose() { }

    [Fact]
    public void DefaultListExists()
    {
        var lists = _repo.LoadAllLists();
        Assert.Single(lists);
        Assert.Equal("Default", lists[0].Name);
    }

    [Fact]
    public void LoadAllByList_EmptyOnStart()
    {
        var defaultList = _repo.LoadAllLists()[0];
        Assert.Empty(_repo.LoadAllByList(defaultList.Id));
    }

    [Fact]
    public void Add_AndLoadAllByList_ReturnsItem()
    {
        var defaultList = _repo.LoadAllLists()[0];
        var item = new TodoItem(Guid.NewGuid(), "Buy milk", false, defaultList.Id);
        _repo.Add(item);

        var items = _repo.LoadAllByList(defaultList.Id);
        Assert.Single(items);
        Assert.Equal(item.Id, items[0].Id);
        Assert.Equal("Buy milk", items[0].Title);
        Assert.False(items[0].IsCompleted);
    }

    [Fact]
    public void Update_ChangesIsCompleted()
    {
        var defaultList = _repo.LoadAllLists()[0];
        var item = new TodoItem(Guid.NewGuid(), "Buy milk", false, defaultList.Id);
        _repo.Add(item);

        _repo.Update(item with { IsCompleted = true });

        Assert.True(_repo.LoadAllByList(defaultList.Id)[0].IsCompleted);
    }

    [Fact]
    public void Delete_RemovesItem()
    {
        var defaultList = _repo.LoadAllLists()[0];
        var item = new TodoItem(Guid.NewGuid(), "Buy milk", false, defaultList.Id);
        _repo.Add(item);

        _repo.Delete(item.Id);

        Assert.Empty(_repo.LoadAllByList(defaultList.Id));
    }

    [Fact]
    public void CreateList_AddsNewList()
    {
        var created = _repo.CreateList("Work");
        var lists = _repo.LoadAllLists();
        Assert.Equal(2, lists.Count);
        Assert.Equal("Work", created.Name);
    }

    [Fact]
    public void DeleteList_RemovesListAndTodos()
    {
        var created = _repo.CreateList("Temp");
        var item = new TodoItem(Guid.NewGuid(), "Task", false, created.Id);
        _repo.Add(item);

        _repo.DeleteList(created.Id);

        Assert.Single(_repo.LoadAllLists()); // only Default remains
        Assert.Empty(_repo.LoadAllByList(created.Id));
    }

    [Fact]
    public void LoadAllByList_PreservesInsertionOrder()
    {
        var defaultList = _repo.LoadAllLists()[0];
        _repo.Add(new TodoItem(Guid.NewGuid(), "First", false, defaultList.Id));
        _repo.Add(new TodoItem(Guid.NewGuid(), "Second", false, defaultList.Id));
        _repo.Add(new TodoItem(Guid.NewGuid(), "Third", false, defaultList.Id));

        var items = _repo.LoadAllByList(defaultList.Id);
        Assert.Equal(["First", "Second", "Third"], items.Select(t => t.Title));
    }
}
