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
            // Only call SelectList for task-mode tabs; naming-mode tabs use a
            // temporary Guid.Empty ListId that is not a valid repository list.
            if (value is not null && value.IsAddingTasks)
            {
                _todoService.SelectList(value.ListId);
            }
        }
    }

    public ReactiveCommand<Unit, Unit> AddTabCommand { get; }
    public ReactiveCommand<TodoListTabViewModel, Unit> SelectTabCommand { get; }

    public MainViewModel(ITodoService todoService)
    {
        _todoService = todoService;

        // Load existing lists into tabs; Take(1) because lists are managed exclusively
        // through this ViewModel — we do not need to react to external list changes.
        // Take(1) also auto-completes the subscription synchronously (BehaviorSubject
        // always has a current value), so no IDisposable cleanup is required here.
        _todoService.Lists
            .Take(1)
            .Subscribe(
                lists =>
                {
                    foreach (var list in lists)
                    {
                        Tabs.Add(new TodoListTabViewModel(_todoService, list.Id, list.Name));
                    }
                    if (Tabs.Count > 0) SelectedTab = Tabs[0];
                },
                ex => System.Diagnostics.Debug.WriteLine($"Failed to load lists: {ex.Message}"));

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
            tab.Dispose(); // dispose the replaced naming-mode placeholder
            SelectedTab = newTab;
            // SelectList is already invoked by the SelectedTab setter above.
        }
    }
}

