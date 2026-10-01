# Task Manager

A personal task manager being developed for a .NET full-stack technical assessment.

Implementation is developed incrementally. M0 establishes the repository scaffold. M1 adds tested Domain and Application task logic, including validation and ownership enforcement. M2 adds SQLite persistence and real database integration tests. Authentication, API endpoints, and frontend functionality remain for later milestones.

## Stack

- .NET 10 / ASP.NET Core Web API / C#
- xUnit backend test projects
- Angular 22 / TypeScript / standalone components / Router / CSS
- SQLite with `Microsoft.Data.Sqlite` and explicit parameterized SQL

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

Domain and Application behavior tests live in `TaskManager.Application.Tests` and use a test-only in-memory repository. `TaskManager.Infrastructure.Tests` exercises real SQLite using a unique temporary database file per test, independent repository connections, and disabled pooling. API tests remain for M4.

## M2 persistence configuration

The infrastructure accepts an externally supplied SQLite connection string through `SqliteConnectionFactory`. For example, a future composition root can read `ConnectionStrings:TaskManager` (environment variable `ConnectionStrings__TaskManager`) with a value such as `Data Source=task-manager.db`. Create the containing directory first when specifying a directory in the path. API wiring is deferred to the API milestone.

Call `SqliteDatabaseInitializer.InitializeAsync` before repository use. It creates the documented Users/Tasks schema inside a transaction with `CREATE TABLE IF NOT EXISTS`; repeated calls preserve existing records. It does not migrate an incompatible pre-existing schema.

Every repository operation opens and disposes its own connection through the factory, which forces `Foreign Keys=True`, even if the supplied string disables foreign keys. Individual task reads, updates, and deletes match both `Id` and `UserId`; lists match `UserId`. Task insert returns a new immutable entity with the SQLite-generated integer ID. Due dates use invariant `yyyy-MM-dd` text, statuses use integers, and absent descriptions use SQL NULL.

`SqliteDemoSeeder.SeedAsync` requires an already generated password hash. In a single transaction it inserts the demo user and three fixed representative tasks only when the demo username is newly inserted. Repeated calls preserve credentials and edited/deleted tasks; an existing user named demo is left untouched. Failure rolls back the entire seed. No usable demo credentials or password hash are provided in M2: framework hashing and final credential population are deferred to M3, and automatic startup composition to M4. Tests use opaque storage fixtures, not usable password hashes.

The minimal M2 user boundary supports insert, lookup by ID, and lookup by username. Usernames are trimmed and SQLite's default case-sensitive uniqueness is retained. Hashes are opaque and must be supplied by a trusted caller; persistence does not hash or verify passwords.
