using TodoApp.Core.Models;

namespace TodoApp.Core.Interfaces;

public interface ITodoService
{
    IObservable<IReadOnlyList<TodoItem>> Todos { get; }
    IObservable<IReadOnlyList<TodoList>> Lists { get; }

    void SelectList(Guid listId);
    TodoList CreateList(string name);
    void DeleteList(Guid listId);

    void Add(string title);
    void Toggle(Guid id);
    void Remove(Guid id);
}
