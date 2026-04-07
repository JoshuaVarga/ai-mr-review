using TodoApp.Core.Models;

namespace TodoApp.Core.Interfaces;

public interface ITodoRepository
{
    IReadOnlyList<TodoList> LoadAllLists();
    TodoList CreateList(string name);
    void DeleteList(Guid listId);

    IReadOnlyList<TodoItem> LoadAllByList(Guid listId);
    void Add(TodoItem item);
    void Update(TodoItem item);
    void Delete(Guid id);
}
