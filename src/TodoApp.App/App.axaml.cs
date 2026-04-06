using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using TodoApp.Core.Interfaces;
using TodoApp.DAL;
using TodoApp.Services;
using TodoApp.UI.ViewModels;
using TodoApp.UI.Views;

namespace TodoApp.App;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TodoApp",
            "todos.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        var services = new ServiceCollection();
        services.AddSingleton<ITodoRepository>(_ => new SqliteTodoRepository(dbPath));
        services.AddSingleton<ITodoService, TodoService>();
        services.AddTransient<MainViewModel>();
        var provider = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                Content = new MainView
                {
                    DataContext = provider.GetRequiredService<MainViewModel>()
                }
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
