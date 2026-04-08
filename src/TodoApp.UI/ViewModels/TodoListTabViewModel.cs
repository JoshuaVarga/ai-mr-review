using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using TodoApp.Core.Interfaces;
using TodoApp.Core.Models;

namespace TodoApp.UI.ViewModels;

public class TodoListTabViewModel : ReactiveObject, IDisposable
{
    private readonly ITodoService _todoService;
    private readonly IDisposable _todosSubscription;

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
        set => this.RaiseAndSetIfChanged(ref _isAddingTasks, value);
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

        // Naming-mode placeholder tabs (Guid.Empty) never display todos;
        // skip the subscription to avoid holding a live observable for an invalid list ID.
        _todosSubscription = listId != Guid.Empty
            ? _todoService.GetTodosForList(listId).Subscribe(todos => Todos = todos)
            : System.Reactive.Disposables.Disposable.Empty;
    }

    public void Dispose()
    {
        _todosSubscription.Dispose();
    }
}
