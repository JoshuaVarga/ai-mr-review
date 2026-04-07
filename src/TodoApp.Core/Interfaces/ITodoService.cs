using TodoApp.Core.Models;

namespace TodoApp.Core.Interfaces;

public interface ITodoService
{
    IObservable<IReadOnlyList<TodoItem>> Todos { get; }
    IObservable<IReadOnlyList<TodoList>> Lists { get; }

    /// <summary>
    /// Returns an observable that emits the current todo list for <paramref name="listId"/>
    /// whenever that list is the active list (i.e. after <see cref="SelectList"/> has been
    /// called with the same ID). Emissions are filtered to the active list only; subscribers
    /// will not receive values while a different list is selected.
    /// </summary>
    IObservable<IReadOnlyList<TodoItem>> GetTodosForList(Guid listId);

    void SelectList(Guid listId);
    TodoList CreateList(string name);
    void DeleteList(Guid listId);

    void Add(string title);
    void Toggle(Guid id);
    void Remove(Guid id);
}
