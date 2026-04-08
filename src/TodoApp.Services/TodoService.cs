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
        if (allLists.Count == 0)
        {
            // Fallback: repository should always provide at least one list; create one if missing.
            var fallback = repository.CreateList("Default");
            _lists = new BehaviorSubject<IReadOnlyList<TodoList>>([fallback]);
            _currentListId = fallback.Id;
        }
        else
        {
            _lists = new BehaviorSubject<IReadOnlyList<TodoList>>([.. allLists]);
            _currentListId = allLists[0].Id;
        }

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
        RefreshLists();
        return list;
    }

    public void DeleteList(Guid listId)
    {
        _repository.DeleteList(listId);
        RefreshLists();

        if (_currentListId == listId)
        {
            var allLists = _lists.Value;
            if (allLists.Count > 0)
            {
                SelectList(allLists[0].Id);
            }
            else
            {
                _currentListId = Guid.Empty;
                _items = [];
                PublishTodos();
            }
        }
    }

    // Emits the current todos whenever the specified list is the active list.
    // In this single-active-list design, a tab only receives updates while its
    // list is selected, which is also the only time its items can be mutated.
    public IObservable<IReadOnlyList<TodoItem>> GetTodosForList(Guid listId) =>
        _todos.AsObservable().Where(_ => _currentListId == listId);

    public void Add(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return;
        if (_currentListId == Guid.Empty) return; // no valid list selected

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

    private void RefreshLists() => _lists.OnNext([.. _repository.LoadAllLists()]);
}
