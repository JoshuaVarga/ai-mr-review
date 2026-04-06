<!-- markdownlint-disable MD013 -->
# AI MR Review

---

## Description

This repository is a simple TODO list app meant to showcase various ways to leverage AI to perform code review.

It contains three branches:

1. **main** - Default branch
2. **feature** - Branch that adds a new feature but purposefully contains a bunch of issues for AI to catch during review
3. **feature-fixes** - Branch that fixes the mistakes made in the feature branch

**Intentional mistakes for MR review to catch:**

| Type | Location | What's wrong |
| --- | --- | --- |
| **Compilation error** | TodoListTabViewModel.cs | Missing `;` on `RaiseAndSetIfChanged` call in `IsAddingTasks` setter |
| **SQL injection** | SqliteTodoRepository.cs | `CreateList` uses string interpolation (`$"...'{name}'..."`) instead of parameterized queries |
| **Debug leftover** | TodoService.cs | `Console.WriteLine` debug log left in `CreateList()` |
| **Messy code** | `TodoApp.Services.csproj` | Unnecessary project reference to `TodoApp.DAL` (Services only uses Core interfaces) |
| **Unused using** | SqliteTodoRepository.cs | `using System.Data;` is imported but never used |
| **No DB Migration** | SqliteTodoRepository.cs | Database schema changes are not handled |

---

## Prerequisites

- .NET 10 SDK
- An AI coding assistant (GitHub Coplilot)

## Set Up

1. Fork the repository with all it's branches
2. Clone your forked repository locally
3. Open the solution with a text editor/IDE that has AI chat capabilites

## MR Review with chat

1. Checkout the branch `feature`
2. Run `git diff main | clip`
3. Paste into chat
4. Paste the following prompt and submit (optionally set it up as a custom propmt)

```text
You are a code reviewer focused on improving code quality and maintainability.

Format each issue you find precisely as:
line=<line_number>: <issue_description>
OR
line=<start_line>-<end_line>: <issue_description>

Check for:
- Unclear or non-conventional naming
- Comment quality (missing or unnecessary)
- Complex expressions needing simplification
- Deep nesting or complex control flow
- Inconsistent style or formatting
- Code duplication or redundancy
- Potential performance issues
- Error handling gaps
- Security concerns
- Breaking of SOLID principles

Multiple issues on one line should be separated by semicolons.

If no issues found, confirm the code is well-written and explain why.
```

## MR Review with built in code review

Some IDE's include [built in code review tools](https://docs.github.com/en/copilot/how-tos/use-copilot-agents/request-a-code-review/use-code-review).
They usually require unstaged changes.
Checkout the `feature` branch and the following to test it out:

```git
git reset HEAD~1
```

## MR Review in the Web browser
