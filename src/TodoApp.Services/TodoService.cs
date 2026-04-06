using System.Reactive.Linq;
using System.Reactive.Subjects;
using TodoApp.Core.Interfaces;
using TodoApp.Core.Models;

namespace TodoApp.Services;

public class TodoService : ITodoService
{
    private readonly ITodoRepository _repository;
    private readonly BehaviorSubject<IReadOnlyList<TodoItem>> _todos;
    private readonly List<TodoItem> _items;

    public IObservable<IReadOnlyList<TodoItem>> Todos => _todos.AsObservable();

    public TodoService(ITodoRepository repository)
    {
        _repository = repository;
        _items = [.. repository.LoadAll()];
        _todos = new([.. _items]);
    }

    public void Add(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return;

        var item = new TodoItem(Guid.NewGuid(), title.Trim(), false);
        _repository.Add(item);
        _items.Add(item);
        Publish();
    }

    public void Toggle(Guid id)
    {
        var index = _items.FindIndex(t => t.Id == id);
        if (index < 0) return;

        var updated = _items[index] with { IsCompleted = !_items[index].IsCompleted };
        _repository.Update(updated);
        _items[index] = updated;
        Publish();
    }

    public void Remove(Guid id)
    {
        _repository.Delete(id);
        _items.RemoveAll(t => t.Id == id);
        Publish();
    }

    private void Publish() => _todos.OnNext([.. _items]);
}
