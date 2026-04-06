using Avalonia.ReactiveUI;
using TodoApp.UI.ViewModels;

namespace TodoApp.UI.Views;

public partial class MainView : ReactiveUserControl<MainViewModel>
{
    public MainView()
    {
        InitializeComponent();
    }
}
