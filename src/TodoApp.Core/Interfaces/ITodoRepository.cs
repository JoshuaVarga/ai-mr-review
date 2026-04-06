using TodoApp.Core.Models;

namespace TodoApp.Core.Interfaces;

public interface ITodoRepository
{
    IReadOnlyList<TodoItem> LoadAll();
    void Add(TodoItem item);
    void Update(TodoItem item);
    void Delete(Guid id);
}
