using Avalonia;
using Avalonia.ReactiveUI;
using TodoApp.App;

AppBuilder.Configure<App>()
    .UsePlatformDetect()
    .UseReactiveUI()
    .StartWithClassicDesktopLifetime(args);
