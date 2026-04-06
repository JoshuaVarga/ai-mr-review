using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using TodoApp.Core.Interfaces;
using TodoApp.Core.Models;

namespace TodoApp.UI.ViewModels;

public class MainViewModel : ReactiveObject
{
    private readonly ITodoService _todoService;

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

    public ReactiveCommand<Unit, Unit> AddTodoCommand { get; }
    public ReactiveCommand<Guid, Unit> ToggleTodoCommand { get; }
    public ReactiveCommand<Guid, Unit> RemoveTodoCommand { get; }

    public MainViewModel(ITodoService todoService)
    {
        _todoService = todoService;

        var canAdd = this.WhenAnyValue(x => x.NewTodoText)
            .Select(text => !string.IsNullOrWhiteSpace(text));

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
