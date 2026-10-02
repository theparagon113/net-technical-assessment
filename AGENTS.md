# AGENTS.md

## Purpose

This file defines the operating rules for AI coding agents working on this repository.

The goal is to keep implementation aligned with the technical assessment, the agreed architecture, and the active milestone.

Agents must prioritize correctness, clarity, security, testability, and scope control over architectural complexity.

---

# Sources of Truth

Before planning or implementing a milestone, read:

1. `docs/ASSESSMENT_REQUIREMENTS.md`
2. `docs/PROJECT_DEFINITION.md`
3. `docs/USER_STORY.md`
4. `docs/DECISIONS.md`
5. `docs/REQUIREMENTS_TRACEABILITY.md`
6. The current milestone instructions

The original external assessment overrides all repository design decisions. `docs/ASSESSMENT_REQUIREMENTS.md` is the repository's canonical transcription of those external requirements. The original PDF remains authoritative; do not require copying or committing it. `docs/REQUIREMENTS_TRACEABILITY.md` records implementation status and does not override requirements.

Conflict priority:

1. Original technical assessment requirements (canonical repository transcription above)
2. `docs/PROJECT_DEFINITION.md`
3. `docs/USER_STORY.md`
4. `docs/DECISIONS.md`
5. Current milestone instructions
6. Implementation convenience

Do not override a higher-priority requirement without explicit developer approval. Ambiguous wording such as "second", "additional", "separate", or "authorized" must not be collapsed into a simpler interpretation without explicit developer approval. If an existing decision conflicts with the assessment, stop following the stale decision, report the conflict, and correct it within authorized scope; otherwise obtain developer direction.

A milestone may not be declared complete until applicable assessment requirements are checked against the traceability matrix. Update traceability when milestone status changes. Historical completion reports must not be rewritten to pretend a corrected interpretation existed earlier.

---

# Scope Control

Implement only the currently requested milestone.

Do not:

- begin future milestones;
- add features not required by the current milestone;
- redesign unrelated code;
- perform speculative refactors;
- add infrastructure for hypothetical future requirements;
- introduce abstractions without a current need.

If something outside the current milestone appears necessary, report it instead of implementing it automatically.

---

# Architecture

The application follows a lightweight Clean Architecture.

Expected dependency direction:

```text
Domain
  ↑
Application
  ↑
Infrastructure
  ↑
  ├── TaskManager.Api
  └── TaskManager.Auth.Api
```

More precisely:

- Domain must not depend on Application, Infrastructure, API, or frontend code.
- Application may depend on Domain.
- Infrastructure may depend on Application and Domain.
- API composes dependencies and translates HTTP requests/responses.
- Business rules belong in Application or Domain, not controllers.
- Database-specific implementation belongs in Infrastructure.
- Frontend must communicate with the backend only through documented HTTP APIs.

Do not create additional architectural layers unless there is a demonstrated requirement.

---


## Two API hosts

The outer presentation/API layer consists of TWO separate executable ASP.NET Core hosts:

1. `TaskManager.Api`: task/data CRUD only, protected by JWT; ownership derives from validated claims, never frontend UserId.
2. `TaskManager.Auth.Api`: registration, login, the explicit authorized current-user endpoint, and the explicit non-authorized public endpoint.

Both reuse the same Domain/Application/Infrastructure boundaries and one shared SQLite database. Do not duplicate business logic, inner projects, databases, or token generation. Both use the MVC/Web API controller pipeline: AddControllers, MapControllers, [ApiController], ControllerBase, and attribute routing. Do not implement primary assessment endpoints as Minimal APIs or add Razor views without an actual requirement.

Both validate JWTs with the same issuer, logical backend audience, signing key, and validation rules; Auth.Api issues tokens through existing shared services. Signing secrets stay outside source control. M4 composition must require one externally configured absolute SQLite Data Source for both hosts, independent of working/content root. Both may initialize the idempotent schema; Auth.Api alone owns demo seeding, preserving existing credentials/task edits/deletions. Angular requires separate auth/task API base URLs.

## Source organization

Preserve the existing lightweight Clean Architecture boundaries.

Within each architectural layer, organize source files by cohesive feature or technical capability rather than allowing project roots to become flat mixed-purpose directories.

Guidelines:

- Application should prefer feature-oriented grouping, such as `Authentication` and `Tasks`.
- Infrastructure should prefer capability-oriented grouping, such as `Authentication` and `Persistence`.
- Persistence-specific repository implementations may live under `Persistence/Repositories`.
- Domain may remain flat while the number of domain types is small.
- Keep namespaces aligned with folder structure.
- Avoid folders that contain only a trivial single type unless they represent a meaningful module boundary.
- Do not introduce new architectural patterns or abstractions solely to justify folder structure.
- Physical organization must improve navigability without changing architectural dependencies or behavior.

When adding new functionality, place it in the existing cohesive module whenever one already exists instead of adding unrelated files to the project root.

Before completing a milestone, inspect the affected project tree and avoid leaving multiple related source files scattered at the project root when a clear cohesive module already exists.

---

# Explicitly Forbidden Technologies

The technical assessment prohibits:

- Entity Framework
- Dapper
- MediatR

Do not add them directly or indirectly.

Do not create custom abstractions whose primary purpose is to reproduce MediatR or an ORM.

---

# Persistence

SQLite is the selected storage technology.

Use:

- `Microsoft.Data.Sqlite`
- explicit SQL
- parameterized queries
- explicit result mapping
- explicit database initialization

Never construct SQL statements using concatenated user input.

Correct:

```csharp
command.CommandText = """
    SELECT Id, Title
    FROM Tasks
    WHERE Id = @id AND UserId = @userId;
    """;

command.Parameters.AddWithValue("@id", id);
command.Parameters.AddWithValue("@userId", userId);
```

Incorrect:

```csharp
command.CommandText =
    $"SELECT * FROM Tasks WHERE Id = {id}";
```

Repository implementations belong in Infrastructure.

Repository abstractions should represent actual application needs.

Prefer:

```text
ITaskRepository
IUserRepository
```

Do not introduce:

```text
IRepository<T>
```

unless explicitly approved.

---

# Application Design

Prefer focused application services such as:

```text
TaskService
AuthService
```

Do not introduce CQRS handlers, mediator pipelines, command buses, or one-class-per-operation patterns unless current complexity clearly requires them.

Keep business rules independent from:

- ASP.NET Core HTTP objects;
- SQLite types;
- frontend models;
- infrastructure configuration.

---

# Authentication and Security

Authentication uses JWT Bearer tokens.

Passwords must:

- never be stored in plaintext;
- never be logged;
- never be returned by API responses;
- be hashed using supported framework functionality.

Authorization must be enforced server-side.

Never trust a `UserId` provided by the frontend for ownership decisions.

The authenticated user identity must come from validated authentication claims.

Task access must always be scoped to the authenticated user.

When querying a resource owned by a user, prefer ownership-aware queries such as:

```sql
WHERE Id = @id
AND UserId = @userId
```

rather than retrieving another user's record and filtering it only afterward.

Do not commit secrets.

Do not weaken authentication or authorization to simplify tests.

---

# Validation

Business validation belongs primarily in the Application layer.

The API may perform HTTP/model-binding validation but must not become the source of business rules.

Validation failures should produce predictable API responses.

Use ASP.NET Core mechanisms such as `ProblemDetails` where appropriate.

---

# Testing

The assessment requires testing of:

- business logic;
- data access;
- API endpoints.

Testing must validate behavior, not implementation details unnecessarily.

## Application Tests

Prefer fast unit tests against application abstractions.

Important business behavior should be developed using TDD where practical.

Expected cycle:

```text
1. Add or modify a test.
2. Run it and confirm the expected failure.
3. Implement the minimum behavior required.
4. Run the test and confirm success.
5. Refactor if useful.
6. Run the affected test suite again.
```

Do not fabricate test-first history.

## Infrastructure Tests

Repository tests should exercise real SQLite behavior.

Do not mock `SqliteConnection` merely to claim repository coverage.

Use isolated temporary or in-memory databases as appropriate.

## API Tests

Prefer real ASP.NET Core integration tests through the application's HTTP pipeline.

Test authentication, authorization, validation, HTTP status codes, and user isolation in both hosts. Prove that an Auth.Api-issued JWT is accepted by TaskManager.Api under shared test configuration. Preserve Application unit tests and real SQLite Infrastructure tests; do not inflate coverage with redundant direct tests of thin controllers.

---

# Frontend

The frontend uses Angular 22 or the current stable Angular 22 release selected for the project.

Use built-in Angular capabilities where practical:

- standalone components;
- Router;
- `HttpClient`;
- reactive forms;
- interceptors;
- route guards;
- signals where useful.

Do not introduce NgRx or another state-management framework without an actual need.

Do not add frontend libraries solely to avoid writing a small amount of simple code.

The UI should remain:

- responsive;
- accessible enough for normal keyboard/form use;
- clear;
- free of known browser-console errors.

---

# Dependencies

Before adding any dependency, ask:

1. Is it required by the assessment?
2. Does the framework already provide the functionality?
3. Is the dependency simpler and safer than implementing the behavior ourselves?
4. Does it introduce concepts the developer will need to explain during code review?

Do not add dependencies casually.

If a new dependency is genuinely justified, mention it in the milestone completion report.

---

# Documentation

Do not silently rewrite project decisions.

If implementation introduces a meaningful architectural or technical decision, flag it for inclusion in:

```text
docs/DECISIONS.md
```

If an AI-generated suggestion is:

- rejected;
- corrected;
- security-hardened;
- simplified;
- materially changed;

flag it as evidence for:

```text
docs/GENAI.md
```

Do not invent GenAI corrections or decisions retrospectively.

---

# Code Quality

Favor:

- clear naming;
- small cohesive methods;
- explicit behavior;
- minimal abstractions;
- predictable control flow;
- framework conventions;
- readable SQL;
- simple dependency injection.

Avoid:

- clever code;
- unnecessary indirection;
- speculative extensibility;
- premature optimization;
- excessive comments explaining obvious code.

Comments should explain why, not restate what the code does.

---

# Build and Validation

Before declaring a backend milestone complete, run the relevant commands, including:

```bash
dotnet build
dotnet test
```

When frontend code exists, also run the appropriate Angular commands, such as:

```bash
npm run build
```

and relevant frontend tests if configured.

Do not report a milestone as complete if required validation is failing.

---

# Milestone Completion Report

After completing a milestone, stop.

Do not automatically continue to the next milestone.

Report:

## Implemented

Brief description of behavior added.

## Files Changed

List the relevant files or areas modified.

## Tests Added or Updated

Explain what behavior is covered.

## Validation Performed

List the commands executed and their results.

## Assumptions

State any assumptions made.

## Deviations

State any deviation from:

- the project definition;
- user story;
- milestone instructions;
- existing decisions.

If there were no deviations, explicitly say so.

## Human Review Items

Highlight anything the developer should inspect or decide before the next milestone.

---

# Final Principle

For every implementation decision, prefer the smallest solution that is:

- correct;
- secure;
- testable;
- maintainable;
- easy to explain during technical review.

This assessment is not an architecture competition.

Do not add complexity unless it solves a real requirement.
