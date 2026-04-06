
# Copilot Instructions — Avalonia + ReactiveUI Todo App

## Repository Purpose

This repository is a **simple reference application** for building a cross-platform desktop **Todo List app** using:

- **Avalonia UI**
- **ReactiveUI**
- **Latest .NET (not .NET 8 — always target the newest stable version available)**

The goal is clarity and simplicity — not a large framework. Keep architecture clean, minimal, and easy to extend.

---

## Solution Structure (Recommended)

```
TodoApp.Core            → Models, interfaces, shared logic
TodoApp.Services        → Business logic (task management)
TodoApp.UI              → Avalonia UI (Views + ViewModels)
TodoApp.App             → Application entry point, DI setup
TodoApp.Tests           → Unit tests
```

### Dependency Direction

```
App → UI, Services, Core
UI  → Core, Services (via interfaces)
Services → Core
Tests → Any project
```

---

## Target Framework

- Use **latest .NET version** (e.g., `net9.0`, `net10.0`, etc.)
- Do NOT target `.NET 8`

All projects should include:

```xml
<Nullable>enable</Nullable>
<ImplicitUsings>enable</ImplicitUsings>
```

---

## Core Principles

- Keep things **simple and readable**
- Prefer **ReactiveUI patterns**
- Avoid over-engineering
- Optimize for **learning and iteration speed**

---

## Models

- Use **immutable records** where possible:

```csharp
public record TodoItem(Guid Id, string Title, bool IsCompleted);
```

- Avoid adding logic to models — keep them as data containers

---

## Services

- Contain business logic (e.g., add/remove/complete todos)
- Expose interfaces in `Core`, implement in `Services`

Example:

```csharp
public interface ITodoService
{
    IObservable<IReadOnlyList<TodoItem>> Todos { get; }

    void Add(string title);
    void Toggle(Guid id);
    void Remove(Guid id);
}
```

- Use **Reactive patterns (Subject / Observable)** for state

---

## ReactiveUI Guidelines

### ViewModels

- Inherit from `ReactiveObject`
- Use `ReactiveCommand` for actions
- Use `ObservableAsPropertyHelper` for derived state if needed

Example:

```csharp
public class MainViewModel : ReactiveObject
{
    public ReactiveCommand<Unit, Unit> AddTodo { get; }

    private string _newTodoText = "";
    public string NewTodoText
    {
        get => _newTodoText;
        set => this.RaiseAndSetIfChanged(ref _newTodoText, value);
    }
}
```

---

### State Flow

- Services expose **IObservable streams**
- ViewModels subscribe and bind to UI
- Avoid manual event wiring

---

## Avalonia UI

### Views

- Use `.axaml` files
- Bind to ViewModels via `DataContext`

```xml
<TextBox Text="{Binding NewTodoText}" />
<Button Command="{Binding AddTodo}" Content="Add" />
```

### ViewModel Wiring

- Use simple DI or manual wiring in `App.axaml.cs`
- Avoid service locator patterns if possible, but keep things pragmatic

---

## Dependency Injection

Keep DI **minimal and straightforward**:

```csharp
services.AddSingleton<ITodoService, TodoService>();
services.AddTransient<MainViewModel>();
```

- Configure in `App` startup
- No complex ServiceCollector pattern needed

---

## Async & Threading

- Prefer async/await when needed
- ReactiveUI already handles most UI threading
- Avoid blocking calls

---

## Testing

- Use **xUnit or NUnit**
- Test:
  - Services (core logic)
  - ViewModel behavior (optional but useful)

---

## Adding Features (Todo App Context)

1. Update or add a model in `Core`
2. Extend service interface + implementation
3. Update ViewModel logic
4. Bind new behavior in Avalonia View

---

## Do / Avoid Summary

### Do

- Use ReactiveUI patterns consistently
- Keep ViewModels thin and reactive
- Keep services focused and testable
- Use latest .NET version
- Keep architecture simple

### Avoid

- Overly complex architecture
- Mixing UI logic into services
- Blocking the UI thread
- Targeting .NET 8
- Adding unnecessary abstraction layers

---

## Scope Reminder

This is a **simple Todo app**, not a full enterprise framework.

Prioritize:
- Simplicity
- Clarity
- Maintainability

