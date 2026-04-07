using System.Reactive;
using System.Reactive.Linq;
using System.Collections.ObjectModel;
using ReactiveUI;
using TodoApp.Core.Interfaces;
using TodoApp.Core.Models;

namespace TodoApp.UI.ViewModels;

public class MainViewModel : ReactiveObject
{
    private readonly ITodoService _todoService;

    public ObservableCollection<TodoListTabViewModel> Tabs { get; } = [];

    private TodoListTabViewModel? _selectedTab;
    public TodoListTabViewModel? SelectedTab
    {
        get => _selectedTab;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedTab, value);
            if (value is not null && value.IsAddingTasks)
            {
                _todoService.SelectList(value.ListId);
            }
        }
    }

    public ReactiveCommand<Unit, Unit> AddTabCommand { get; }
    public ReactiveCommand<TodoListTabViewModel, Unit> SelectTabCommand { get; }

    private string _newListName = "";
    public string NewListName
    {
        get => _newListName;
        set => this.RaiseAndSetIfChanged(ref _newListName, value);
    }

    public MainViewModel(ITodoService todoService)
    {
        _todoService = todoService;

        // Load existing lists into tabs
        _todoService.Lists
            .Take(1)
            .Subscribe(lists =>
            {
                foreach (var list in lists)
                {
                    Tabs.Add(new TodoListTabViewModel(_todoService, list.Id, list.Name));
                }
                if (Tabs.Count > 0) SelectedTab = Tabs[0];
            });

        AddTabCommand = ReactiveCommand.Create(() =>
        {
            // Add a placeholder tab in "naming" mode
            var tempId = Guid.Empty;
            var tab = new TodoListTabViewModel(_todoService, tempId, "New List", isAddingTasks: false);
            Tabs.Add(tab);
            SelectedTab = tab;
        });

        SelectTabCommand = ReactiveCommand.Create<TodoListTabViewModel>(tab =>
        {
            SelectedTab = tab;
        });
    }

    public void ConfirmNewList(TodoListTabViewModel tab, string name)
    {
        var created = _todoService.CreateList(name);
        var index = Tabs.IndexOf(tab);
        if (index >= 0)
        {
            var newTab = new TodoListTabViewModel(_todoService, created.Id, created.Name);
            Tabs[index] = newTab;
            SelectedTab = newTab;
            _todoService.SelectList(created.Id);
        }
    }
}

