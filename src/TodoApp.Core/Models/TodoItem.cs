namespace TodoApp.Core.Models;

public record TodoItem(Guid Id, string Title, bool IsCompleted);
