# Technical Decisions

This document records meaningful technical decisions made during the development of the technical assessment.

Its purpose is to preserve the reasoning behind the implementation so those decisions can be reviewed, explained, and defended during the technical presentation and code review.

This is not intended to document every implementation detail.

A decision belongs here when:

- multiple reasonable alternatives existed;
- the choice materially affects architecture, security, testing, maintainability, or developer experience;
- the decision may reasonably come up during code review.

---

# DEC-001 — Use SQLite as the Data Store

## Status

Accepted

## Context

The assessment requires a database or data-storage solution but does not require a specific database engine.

Entity Framework and Dapper are explicitly prohibited.

The application is a small task-management system with:

- users;
- tasks;
- simple ownership relationships;
- low expected concurrency;
- no distributed-storage requirements.

## Decision

Use SQLite through `Microsoft.Data.Sqlite`.

SQL will be written explicitly and executed using parameterized commands.

## Rationale

SQLite provides:

- relational storage appropriate for the domain;
- primary keys and foreign keys;
- transactions;
- SQL constraints;
- direct SQL access;
- very low setup overhead;
- easy local execution;
- straightforward isolated integration testing.

It also allows the project to demonstrate explicit database interaction without relying on an ORM.

## Alternatives Considered

### SQL Server

Advantages:

- strongly aligned with the .NET enterprise ecosystem;
- representative of many production systems.

Not selected because:

- it adds unnecessary local setup;
- it makes evaluator setup more complex;
- its additional capabilities are not required by this application.

### PostgreSQL

Advantages:

- mature relational database;
- strong SQL capabilities.

Not selected because:

- it requires an additional database service or container;
- the exercise does not require features that justify that operational dependency.

### NoSQL / Document Database

Advantages:

- valid modern storage option;
- official drivers can be used without an ORM.

Not selected because:

- the domain is naturally relational;
- it would introduce additional modeling concepts without solving a requirement;
- it would increase the surface area that must be explained during review.

## Trade-offs

SQLite is not intended for the same concurrency and distributed workloads as a server-based database.

That limitation is acceptable for the assessment.

Database access is isolated behind application interfaces so another persistence implementation could replace SQLite without changing business logic.

---

# DEC-002 — Use Explicit SQL Instead of an ORM

## Status

Accepted

## Context

The assessment explicitly prohibits Entity Framework and Dapper.

The data layer must demonstrate direct interaction with the selected data store.

## Decision

Use `Microsoft.Data.Sqlite` directly with:

- `SqliteConnection`;
- `SqliteCommand`;
- parameterized queries;
- explicit result mapping.

## Rationale

This makes data-access behavior visible and demonstrates:

- SQL knowledge;
- parameter handling;
- SQL-injection prevention;
- connection/resource management;
- mapping between persisted data and application models.

It also satisfies the assessment without introducing another abstraction that effectively recreates an ORM.

## Trade-offs

Explicit SQL requires more manual mapping and boilerplate.

For the small number of entities and queries in this project, that cost is acceptable and keeps behavior transparent.

---

# DEC-003 — Use Angular 22 for the Frontend

## Status

Accepted

## Context

The assessment allows a frontend framework of the developer's choice and provides React and Vue only as examples.

The developer has prior experience with Angular and has previously discussed Angular experience during the interview process.

## Decision

Use Angular 22, using the stable release selected at project creation.

## Rationale

Angular provides built-in solutions for the project's required frontend concerns:

- dependency injection;
- routing;
- HTTP communication;
- reactive forms;
- interceptors;
- route guards;
- reactive state primitives.

Choosing a familiar framework reduces implementation risk and makes the frontend easier to explain during the live code review.

## Alternatives Considered

### React

React could provide a smaller initial scaffold and would also satisfy the assessment.

It was not selected because framework familiarity and code-review confidence provide more value for this assessment than minimizing framework structure.

## Trade-offs

Angular has more framework structure than a minimal React application.

For this project, that structure is acceptable because the required application remains small and uses mostly built-in functionality.

---

# DEC-004 — Use Lightweight Clean Architecture

## Status

Accepted

## Context

The assessment explicitly evaluates Clean Architecture and separation of concerns.

At the same time, the application itself is intentionally small.

## Decision

Use four main backend boundaries:

```text
Domain
Application
Infrastructure
API
```

Keep each layer limited to responsibilities required by the current system.

## Rationale

This provides clear dependency boundaries while avoiding architecture that is disproportionate to the application.

Business logic remains independent from:

- HTTP;
- SQLite;
- framework-specific persistence details.

## Alternatives Considered

A more elaborate implementation could include:

- CQRS;
- commands and queries;
- mediator pipelines;
- generic repositories;
- specification patterns;
- Unit of Work;
- additional domain abstractions.

These were not selected because they do not currently solve a real requirement.

## Trade-offs

If the application became significantly larger, some responsibilities currently grouped inside application services might eventually need to be separated.

That complexity is not justified by the current scope.

---

# DEC-005 — Use Specific Repository Interfaces

## Status

Accepted

## Context

The Application layer requires persistence abstractions.

A generic repository is a common pattern but can become an unnecessary abstraction over the actual persistence needs of a domain.

## Decision

Use focused repository abstractions such as:

```text
ITaskRepository
IUserRepository
```

Do not use:

```text
IRepository<T>
```

## Rationale

Specific interfaces:

- make application requirements explicit;
- expose only operations actually needed by the system;
- avoid leaking persistence abstractions into business logic;
- avoid recreating generic ORM behavior.

## Trade-offs

There may be small amounts of repeated interface structure.

The clarity is more valuable than removing a few repeated method shapes.

---

# DEC-006 — Use Application Services Instead of CQRS/MediatR

## Status

Accepted

## Context

The application contains a limited set of business operations around authentication and task management.

The assessment prohibits MediatR.

## Decision

Use focused application services:

```text
AuthService
TaskService
```

Do not implement a custom mediator or a one-handler-per-operation CQRS architecture.

## Rationale

Application services provide enough separation for the current complexity.

They are:

- easy to test;
- easy to navigate;
- easy to explain;
- compatible with Clean Architecture;
- free of unnecessary dispatching infrastructure.

## Trade-offs

If the number and complexity of application workflows grew substantially, splitting behavior into dedicated use cases could become appropriate.

That complexity is not present in the current assessment.

---

# DEC-007 — Use JWT Bearer Authentication

## Status

Accepted

## Context

The system consists of an Angular SPA communicating with an ASP.NET Core API.

The assessment requires:

- user creation;
- login;
- authorized endpoints;
- non-authorized endpoints.

## Decision

Use JWT Bearer authentication.

The token identifies the authenticated user and is validated by the backend.

## Rationale

JWT provides a simple and explicit authentication mechanism between a standalone SPA and API.

ASP.NET Core provides first-class JWT Bearer support.

It also makes authenticated API behavior straightforward to demonstrate and test.

## Trade-offs

Browser token storage requires careful handling.

For this assessment, a short-lived token and simple client-side session handling are sufficient.

A production application with stronger session requirements could use a different architecture, such as secure HttpOnly cookies or a BFF.

---

# DEC-008 — Do Not Implement Refresh Tokens

## Status

Accepted

## Context

Refresh-token rotation adds:

- additional persistence;
- token-family handling;
- revocation behavior;
- replay considerations;
- additional security-sensitive code.

The assessment requires authentication but does not require long-lived sessions.

## Decision

Use a reasonably short-lived JWT without refresh tokens.

## Rationale

This satisfies the authentication and authorization requirements while keeping security-sensitive scope proportional to the exercise.

## Trade-offs

Users must log in again after token expiration.

That is acceptable for the assessment application.

---

# DEC-009 — Enforce Task Ownership Server-Side

## Status

Accepted

## Context

Tasks belong to users.

The client cannot be trusted to determine which resources the current user owns.

## Decision

All task access must use the authenticated server-side user identity.

Task queries for individual resources should normally use both:

```text
TaskId
UserId
```

as query conditions.

The API must never authorize access based on a `UserId` supplied by the frontend.

## Rationale

This prevents horizontal authorization vulnerabilities where one authenticated user accesses another user's records by changing an identifier.

Filtering ownership at the persistence query boundary also avoids unnecessarily loading another user's data.

## Trade-offs

An inaccessible resource and a nonexistent resource will generally produce the same `404 Not Found` behavior.

This is intentional because it avoids disclosing whether another user's resource exists.

---

# Adding Future Decisions

Use the following template:

```text
# DEC-XXX — Decision Title

## Status

Proposed | Accepted | Superseded

## Context

What problem or choice required a decision?

## Decision

What was chosen?

## Rationale

Why was it chosen?

## Alternatives Considered

What realistic alternatives were evaluated?

## Trade-offs

What disadvantages or limitations were accepted?
```

Only record meaningful decisions.

Do not create a decision entry for every class, method, endpoint, or implementation detail.