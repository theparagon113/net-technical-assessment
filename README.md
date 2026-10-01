# Task Manager

A personal task manager being developed for a .NET full-stack technical assessment.

Implementation is developed incrementally. M0 establishes the repository scaffold only; authentication, task management, and persistence are not implemented yet.

## Stack

- .NET 10 / ASP.NET Core Web API / C#
- xUnit backend test projects
- Angular 22 / TypeScript / standalone components / Router / CSS
- Planned persistence: SQLite with `Microsoft.Data.Sqlite` and explicit SQL

## Repository structure

```text
TaskManager.sln
src/
  backend/
    TaskManager.Domain/
    TaskManager.Application/
    TaskManager.Infrastructure/
    TaskManager.Api/
  frontend/
    task-manager-web/
tests/
  TaskManager.Application.Tests/
  TaskManager.Infrastructure.Tests/
  TaskManager.Api.Tests/
docs/
  PROJECT_DEFINITION.md
  USER_STORY.md
  DECISIONS.md
```

## Project requirements

- [Project definition](docs/PROJECT_DEFINITION.md)
- [User story](docs/USER_STORY.md)
- [Technical decisions](docs/DECISIONS.md)

## Scaffold validation

From the repository root:

```sh
dotnet restore
dotnet build
dotnet test
```

From `src/frontend/task-manager-web`:

Use a supported Node version: `^22.22.3`, `^24.15.0`, or `>=26.0.0`.

```sh
npm install
npm run build
npm test -- --watch=false
```

Backend test projects are intentionally empty until behavior is introduced in later milestones.
