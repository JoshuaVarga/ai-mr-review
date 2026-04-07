using System.Reactive.Linq;
using System.Reactive.Subjects;
using TodoApp.Core.Interfaces;
using TodoApp.Core.Models;

namespace TodoApp.Services;

public class TodoService : ITodoService
{
    private readonly ITodoRepository _repository;
    private readonly BehaviorSubject<IReadOnlyList<TodoItem>> _todos;
    private readonly BehaviorSubject<IReadOnlyList<TodoList>> _lists;
    private List<TodoItem> _items;
    private Guid _currentListId;

    public IObservable<IReadOnlyList<TodoItem>> Todos => _todos.AsObservable();
    public IObservable<IReadOnlyList<TodoList>> Lists => _lists.AsObservable();

    public TodoService(ITodoRepository repository)
    {
        _repository = repository;

        var allLists = repository.LoadAllLists();
        _lists = new BehaviorSubject<IReadOnlyList<TodoList>>([.. allLists]);

        // default to first list
        _currentListId = allLists[0].Id;
        _items = [.. repository.LoadAllByList(_currentListId)];
        _todos = new BehaviorSubject<IReadOnlyList<TodoItem>>([.. _items]);
    }

    public void SelectList(Guid listId)
    {
        _currentListId = listId;
        _items = [.. _repository.LoadAllByList(listId)];
        PublishTodos();
    }

    public TodoList CreateList(string name)
    {
        var list = _repository.CreateList(name);
        var allLists = _repository.LoadAllLists();
        _lists.OnNext([.. allLists]);
        Console.WriteLine($"DEBUG: Created list {list.Name} with id {list.Id}");
        return list;
    }

    public void DeleteList(Guid listId)
    {
        _repository.DeleteList(listId);
        var allLists = _repository.LoadAllLists();
        _lists.OnNext([.. allLists]);

        if (_currentListId == listId && allLists.Count > 0)
        {
            SelectList(allLists[0].Id);
        }
    }

    public void Add(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return;

        var item = new TodoItem(Guid.NewGuid(), title.Trim(), false, _currentListId);
        _repository.Add(item);
        _items.Add(item);
        PublishTodos();
    }

    public void Toggle(Guid id)
    {
        var index = _items.FindIndex(t => t.Id == id);
        if (index < 0) return;

        var updated = _items[index] with { IsCompleted = !_items[index].IsCompleted };
        _repository.Update(updated);
        _items[index] = updated;
        PublishTodos();
    }

    public void Remove(Guid id)
    {
        _repository.Delete(id);
        _items.RemoveAll(t => t.Id == id);
        PublishTodos();
    }

    private void PublishTodos() => _todos.OnNext([.. _items]);
}
