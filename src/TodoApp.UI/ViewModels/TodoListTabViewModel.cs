using System.Reactive;
using System.Reactive.Linq;
using System.Collections.ObjectModel;
using ReactiveUI;
using TodoApp.Core.Interfaces;
using TodoApp.Core.Models;

namespace TodoApp.UI.ViewModels;

public class TodoListTabViewModel : ReactiveObject
{
    private readonly ITodoService _todoService;

    public Guid ListId { get; }

    private string _name;
    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    private string _newTodoText = "";
    public string NewTodoText
    {
        get => _newTodoText;
        set => this.RaiseAndSetIfChanged(ref _newTodoText, value);
    }

    private IReadOnlyList<TodoItem> _todos = [];
    public IReadOnlyList<TodoItem> Todos
    {
        get => _todos;
        private set => this.RaiseAndSetIfChanged(ref _todos, value);
    }

    private bool _isAddingTasks = true;
    public bool IsAddingTasks
    {
        get => _isAddingTasks;
        set => this.RaiseAndSetIfChanged(ref _isAddingTasks, value)
    }

    public ReactiveCommand<Unit, Unit> AddTodoCommand { get; }
    public ReactiveCommand<Guid, Unit> ToggleTodoCommand { get; }
    public ReactiveCommand<Guid, Unit> RemoveTodoCommand { get; }

    public TodoListTabViewModel(ITodoService todoService, Guid listId, string name, bool isAddingTasks = true)
    {
        _todoService = todoService;
        ListId = listId;
        _name = name;
        _isAddingTasks = isAddingTasks;

        var canAdd = this.WhenAnyValue(x => x.NewTodoText, x => x.IsAddingTasks)
            .Select(tuple => !string.IsNullOrWhiteSpace(tuple.Item1) && tuple.Item2);

        AddTodoCommand = ReactiveCommand.Create(() =>
        {
            _todoService.Add(NewTodoText);
            NewTodoText = "";
        }, canAdd);

        ToggleTodoCommand = ReactiveCommand.Create<Guid>(id => _todoService.Toggle(id));
        RemoveTodoCommand = ReactiveCommand.Create<Guid>(id => _todoService.Remove(id));

        _todoService.Todos.Subscribe(todos => Todos = todos);
    }
}
