using TodoApp.Core.Models;

namespace TodoApp.Core.Interfaces;

public interface ITodoService
{
    IObservable<IReadOnlyList<TodoItem>> Todos { get; }

    void Add(string title);
    void Toggle(Guid id);
    void Remove(Guid id);
}
