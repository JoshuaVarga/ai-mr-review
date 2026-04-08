using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using TodoApp.UI.ViewModels;

namespace TodoApp.UI.Views;

public partial class MainView : ReactiveUserControl<MainViewModel>
{
    public MainView()
    {
        InitializeComponent();
    }

    private void OnCreateListClick(object? sender, RoutedEventArgs e)
    {
        var textBox = this.FindControl<TextBox>("NewListNameBox");
        if (textBox == null) return;

        var name = textBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name)) return;

        if (ViewModel?.SelectedTab is { IsAddingTasks: false } tab)
        {
            ViewModel.ConfirmNewList(tab, name);
            textBox.Text = "";
        }
    }
}
