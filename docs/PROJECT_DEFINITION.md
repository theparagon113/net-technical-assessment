# Technical Assessment — Project Definition & Execution Plan

**Deadline:** October 4, 2026 — 10:00  
**Repository:** Single public GitHub repository  
**Project:** Personal Task Manager  
**Primary objective:** Deliver a small, complete, secure, tested full-stack application that satisfies the assessment requirements and can be confidently explained during a live code review.

---

# 1. Source of Truth

Implementation decisions should follow this priority:

1. Requirements in the provided technical assessment.
2. This project definition.
3. The current milestone specification.
4. Implementation details chosen during development.

If a coding agent proposes something that conflicts with a higher level, the higher-level requirement wins.

The assessment explicitly requires:

- .NET / C# backend.
- ASP.NET MVC / Web API.
- A database or data store.
- Clean Architecture principles.
- TDD methodology.
- CRUD functionality.
- User creation and login.
- Authorized and non-authorized API behavior.
- Separate data and business logic layers.
- Unit testing of data access, business logic, and API endpoints.
- A frontend framework.
- Responsive and user-friendly UI.
- Seeded data and demo credentials.
- README/setup documentation.
- No Entity Framework, Dapper, or MediatR.  

The assessment additionally evaluates:

- Clean Architecture.
- Test coverage / TDD.
- Code quality.
- Correct functionality.
- Presentation quality.
- GenAI fluency and critical evaluation of generated code.

---

# 2. Product Definition

## Informal User Story

> As a registered user, I want to securely manage my personal tasks so that I can create, review, update, complete, and delete the work I need to track.

A user must be able to:

- register;
- log in;
- view their tasks;
- create a task;
- edit one of their tasks;
- delete one of their tasks;
- change its status;
- log out.

Users must never be able to access or modify another user's tasks.

This domain intentionally mirrors the task-management example included in the GenAI portion of the assessment, which specifies tasks containing a title, description, status, due date, and association with a user.

---

# 3. Scope

## Included

### Authentication

- User registration.
- User login.
- Secure password hashing.
- JWT generation.
- JWT Bearer authentication.
- Protected API routes.
- Anonymous registration and login routes.
- Current-user identification from JWT claims.
- Logout on the frontend.

### Tasks

Each task contains:

- `Id`
- `UserId`
- `Title`
- `Description`
- `Status`
- `DueDate`

Supported operations:

- Create.
- Get all tasks for authenticated user.
- Get task by ID.
- Update.
- Delete.

### Frontend

- Register screen.
- Login screen.
- Task management screen.
- Create task.
- Edit task.
- Delete task.
- Change status.
- Loading feedback.
- Validation feedback.
- API error feedback.
- Responsive layout.

### Documentation

- Root README.
- Setup instructions.
- Architecture explanation.
- Demo credentials.
- GenAI usage document.
- Relevant design decisions.

---

# 4. Explicit Non-Goals

The following will **not** be implemented unless an assessment requirement forces us to reconsider:

- Refresh tokens.
- OAuth/OIDC.
- Social authentication.
- Roles or permissions beyond ownership.
- Email confirmation.
- Password recovery.
- Multiple organizations.
- Shared tasks.
- Comments.
- Attachments.
- Notifications.
- Search.
- Pagination.
- Sorting.
- Filtering beyond what becomes trivially useful.
- Audit history.
- Event sourcing.
- CQRS.
- MediatR or homemade MediatR equivalents.
- Generic repository abstraction.
- Unit of Work abstraction.
- AutoMapper.
- FluentValidation.
- NgRx.
- Microservices.
- Message queues.
- Docker orchestration.
- Cloud deployment.
- Azure services.
- Observability platforms.
- Premature caching.
- Premature performance optimization.

The system should remain intentionally small.

---

# 5. Technology Stack

## Backend

**.NET 10 / ASP.NET Core**

.NET 10 is currently an active LTS release with support through November 2028.

Use:

- C#
- ASP.NET Core Web API controllers.
- Built-in Dependency Injection.
- Built-in JWT Bearer authentication.
- `PasswordHasher<TUser>` for password hashing.
- `Microsoft.Data.Sqlite`.
- Raw parameterized SQL.
- xUnit.

No:

- Entity Framework.
- Dapper.
- MediatR.

---

## Frontend

**Angular 22 + TypeScript**

Angular 22 is currently under active support.

Use:

- Standalone Angular architecture.
- Angular Router.
- `HttpClient`.
- Reactive Forms.
- Functional HTTP interceptor where appropriate.
- Route guard.
- Signals where they naturally simplify local/application state.
- Plain CSS or similarly lightweight styling.

Do not introduce NgRx or another global state library.

---

## Database

**SQLite using `Microsoft.Data.Sqlite` directly.**

Rationale:

- Fully relational.
- Adequate for the application domain.
- No database server required.
- Easy repository setup.
- Excellent integration-test isolation.
- Low operational overhead.
- Supports transactions, constraints and parameterized SQL.
- Keeps explicit database interaction visible for assessment.
- Allows us to demonstrate data-access knowledge without an ORM.

SQLite is an infrastructure choice.

The Application and Domain layers must not depend on SQLite.

A future SQL Server/PostgreSQL implementation should be possible by creating new repository implementations without changing business logic.

---

# 6. Architecture

Use a lightweight Clean Architecture:

```text
                    ┌───────────────────┐
                    │      Angular      │
                    └─────────┬─────────┘
                              │ HTTP
                              ▼
                    ┌───────────────────┐
                    │        API        │
                    │ Controllers/Auth  │
                    └─────────┬─────────┘
                              │
                              ▼
                    ┌───────────────────┐
                    │    Application    │
                    │ Services / Ports  │
                    └───────┬───────────┘
                            │
                   abstractions
                            │
             ┌──────────────┴──────────────┐
             ▼                             ▼
     ITaskRepository                IUserRepository
             ▲                             ▲
             │                             │
             └──────────────┬──────────────┘
                            │
                    ┌───────┴─────────┐
                    │ Infrastructure  │
                    │ SQLite / JWT /  │
                    │ password hash   │
                    └─────────────────┘
```

Dependencies flow inward.

Infrastructure can depend on Application and Domain.

Application can depend on Domain.

Domain depends on nothing application-specific.

API performs composition and HTTP translation but does not contain business rules.

This directly supports the assessment requirement that business logic remain independent of both the API and data layer.

---

# 7. Solution Structure

```text
technical-assessment/
│
├── src/
│   ├── backend/
│   │   ├── TaskManager.Domain/
│   │   ├── TaskManager.Application/
│   │   ├── TaskManager.Infrastructure/
│   │   └── TaskManager.Api/
│   │
│   └── frontend/
│       └── task-manager-web/
│
├── tests/
│   ├── TaskManager.Application.Tests/
│   ├── TaskManager.Infrastructure.Tests/
│   └── TaskManager.Api.Tests/
│
├── docs/
│   └── GENAI.md
│
├── TaskManager.sln
├── README.md
└── .gitignore
```

No Nx.

No monorepo management framework.

The Git repository itself is sufficient.

---

# 8. Domain Model

## User

```text
User
----
Id
Username
PasswordHash
```

Rules:

- Username is required.
- Username must be unique.
- Password is never persisted directly.
- Authentication errors must not expose whether the username or password was incorrect.

---

## Task

```text
TaskItem
--------
Id
UserId
Title
Description
Status
DueDate
```

### Status

Use a fixed enum:

```text
Pending
InProgress
Completed
```

Avoid a separate status table.

---

# 9. Database Schema

Conceptually:

```sql
Users
-----
Id INTEGER PRIMARY KEY AUTOINCREMENT
Username TEXT NOT NULL UNIQUE
PasswordHash TEXT NOT NULL
```

```sql
Tasks
-----
Id INTEGER PRIMARY KEY AUTOINCREMENT
UserId INTEGER NOT NULL
Title TEXT NOT NULL
Description TEXT NULL
Status INTEGER NOT NULL
DueDate TEXT NOT NULL

FOREIGN KEY(UserId) REFERENCES Users(Id)
```

Dates are represented consistently and converted explicitly between SQLite and .NET.

Foreign-key enforcement must be enabled.

Database initialization should be deterministic and automatic for local development.

---

# 10. Data Access Rules

Repositories will contain explicit SQL using `Microsoft.Data.Sqlite`.

All variable input must use SQL parameters.

Never construct SQL like:

```text
"... WHERE Username = '" + username + "'"
```

Every database operation must:

- create/use the required connection;
- use parameterized SQL;
- dispose commands/readers correctly;
- handle nullable values explicitly;
- map database results explicitly;
- return domain/application data rather than SQLite-specific types.

Repository abstractions:

```text
ITaskRepository
IUserRepository
```

Do **not** create:

```text
IRepository<T>
```

The purpose of the repository abstraction is to represent application requirements, not to recreate an ORM.

---

# 11. Application Layer

Use two primary services:

```text
AuthService
TaskService
```

Do not create a separate class for every CRUD verb unless implementation complexity later justifies it.

Potential application abstractions:

```text
IUserRepository
ITaskRepository
IPasswordHasher
ITokenService
```

Infrastructure supplies implementations.

---

# 12. Authentication Design

## Registration

```text
POST /api/auth/register
```

Flow:

```text
Request
 ↓
Validate username/password
 ↓
Verify username does not already exist
 ↓
Hash password
 ↓
Persist user
 ↓
Return created user information
```

Never return `PasswordHash`.

---

## Login

```text
POST /api/auth/login
```

Flow:

```text
Username + Password
       ↓
Find user
       ↓
Verify password hash
       ↓
Generate JWT
       ↓
Return token + user information
```

Invalid credentials return:

```text
401 Unauthorized
```

with a generic message.

---

## Current User

```text
GET /api/auth/me
```

Requires authentication.

Returns information derived from the authenticated identity.

---

## JWT

JWT contains only information required to identify the user, such as:

```text
sub
name
```

Configuration validates:

- signing key;
- issuer;
- audience;
- lifetime;
- signature.

Use a reasonable short lifetime such as approximately one hour.

No refresh-token implementation.

---

# 13. Task API

All task endpoints require authentication.

## List tasks

```text
GET /api/tasks
```

Returns only tasks owned by authenticated user.

Expected:

```text
200 OK
```

Empty collection is valid.

---

## Get task

```text
GET /api/tasks/{id}
```

Expected:

```text
200 OK
404 Not Found
```

A task belonging to another user should not be exposed.

Returning `404` for both missing and inaccessible task IDs avoids disclosing the existence of another user's record.

---

## Create

```text
POST /api/tasks
```

Expected:

```text
201 Created
```

Prefer HTTP semantics such as `CreatedAtAction`.

The authenticated user's ID is obtained from the token.

**Never accept `UserId` from the frontend when creating a task.**

---

## Update

```text
PUT /api/tasks/{id}
```

Expected:

```text
200 OK
400 Bad Request
404 Not Found
```

Only owner can update.

---

## Delete

```text
DELETE /api/tasks/{id}
```

Expected:

```text
204 No Content
404 Not Found
```

Only owner can delete.

---

# 14. Business Validation

Validation belongs primarily in the Application layer.

## Task

Minimum rules:

- Title is required.
- Title is trimmed.
- Title has a reasonable maximum length, e.g. 120 characters.
- Description has a reasonable maximum length, e.g. 1000 characters.
- Status must be valid.
- Due date is required and must parse correctly.
- User ownership is mandatory.

Do not create arbitrary business rules such as forbidding past due dates unless needed by the user story.

---

## User

- Username required.
- Username normalized consistently.
- Username unique.
- Password required.
- Reasonable minimum password requirements.
- Password stored only as a secure hash.

---

# 15. Error Handling

API responses should be predictable.

Prefer ASP.NET Core `ProblemDetails` for application/API errors.

At minimum handle:

```text
Validation failure      → 400
Authentication failure  → 401
Missing/inaccessible    → 404
Duplicate username      → 409
Unexpected failure      → 500
```

Unexpected internal details must not be returned to the client.

No stack traces in normal API responses.

---

# 16. Security Requirements

Minimum security baseline:

- Password hashing using supported framework functionality.
- Parameterized SQL exclusively.
- JWT signature and lifetime validation.
- CORS limited to required frontend origins.
- User ownership enforced server-side.
- No secrets checked into Git.
- No password hashes returned by API.
- No user IDs trusted from frontend authorization decisions.
- Generic login failure responses.
- Input size limits through validation.
- HTTPS configuration retained where appropriate.

For the SPA, storing the short-lived demo JWT in session storage is acceptable for this assessment, with the limitation documented.

A production application could instead use a stronger browser-token architecture such as secure HttpOnly cookies/BFF depending on system requirements.

No refresh-token system will be added solely to improve the assessment.

---

# 17. Seed Data

Application must automatically provide demo data.

Example:

```text
Username: demo
Password: Demo123!
```

The demo password is intentionally public test data, not a production secret.

The password must still be stored as a hash.

Seed:

- demo user;
- 2–3 representative tasks.

Seeding should be idempotent.

The assessment explicitly requests seeded data / credentials for demonstration purposes.

---

# 18. Angular Application

## Routes

Minimum:

```text
/login
/register
/tasks
```

`/tasks` requires authentication.

Anonymous users attempting to access it should be redirected to `/login`.

---

## Suggested structure

```text
src/app/
│
├── core/
│   └── auth/
│       ├── auth.service.ts
│       ├── auth.interceptor.ts
│       └── auth.guard.ts
│
├── tasks/
│   ├── task-list/
│   ├── task-form/
│   ├── task.service.ts
│   └── task.model.ts
│
├── shared/
│
├── app.component.ts
└── app.routes.ts
```

Keep `shared` empty unless something genuinely becomes reusable.

Delete it if unnecessary.

---

# 19. Angular State Strategy

No centralized global state library.

Use:

- services for API access;
- signals for simple reactive application/local state where useful;
- component state for UI-only concerns.

Example:

```text
AuthService
    authenticated user
    token/auth state

TaskService
    API communication

TaskListComponent
    task collection
    loading/error state

TaskFormComponent
    reactive form
```

---

# 20. Frontend UX

The UI does not need to be visually elaborate.

It **does** need to appear intentional and complete.

Minimum UX:

- Clearly labeled forms.
- Responsive layout.
- Visible validation.
- Disabled submit while invalid/submitting.
- Loading states.
- Error feedback.
- Empty-state message.
- Confirmation before destructive deletion.
- Obvious logout.
- Appropriate keyboard/form behavior.
- No browser-console errors.

The assessment explicitly evaluates responsiveness, usability and structured frontend code.

---

# 21. Testing Strategy

The assessment specifically requests tests for data access, business logic and API endpoints.

Tests should prove useful behavior rather than maximize an arbitrary percentage.

## Application Tests

Fast unit tests.

Use fakes/mocks around repository abstractions.

Critical scenarios:

### Auth

- Valid registration.
- Duplicate username rejected.
- Password is hashed before persistence.
- Successful login.
- Invalid password rejected.
- Unknown user rejected.

### Tasks

- Create valid task.
- Invalid title rejected.
- Invalid status rejected.
- Retrieve user's tasks.
- User cannot retrieve another user's task.
- User cannot update another user's task.
- User cannot delete another user's task.
- Update missing task.
- Delete missing task.

---

# 22. Infrastructure Tests

Repository tests should execute against actual SQLite behavior rather than mocking `SqliteConnection`.

Use an isolated temporary/in-memory database per fixture/test strategy.

Test:

- Insert user.
- Unique username constraint.
- Find user.
- Insert task.
- Get tasks by owner.
- Get task by ID and owner.
- Update.
- Delete.
- Foreign-key behavior.

The purpose is to validate actual SQL and mapping.

---

# 23. API Tests

Use ASP.NET Core integration testing such as `WebApplicationFactory`.

Test representative HTTP behavior:

```text
POST /auth/register        → 201
POST /auth/login           → 200
invalid login              → 401
GET /auth/me anonymous     → 401
GET /auth/me authenticated → 200

GET /tasks anonymous       → 401
POST /tasks                → 201
GET /tasks                 → 200
PUT /tasks/{id}            → 200
DELETE /tasks/{id}         → 204
missing task               → 404
invalid request            → 400
```

Also prove cross-user isolation with at least one API integration test.

---

# 24. Frontend Tests

Frontend automated testing is useful but secondary to the backend testing explicitly requested by the assessment.

If time permits, cover critical pieces such as:

- authentication service;
- interceptor;
- task service;
- simple component behavior.

Do not sacrifice required backend functionality or documentation solely to increase frontend test count.

---

# 25. TDD Strategy

The assessment says TDD is preferred.

We should perform **real TDD for important business behavior**, rather than merely writing tests after everything is finished.

For relevant milestones:

```text
1. Write behavior test.
2. Run it and verify failure.
3. Implement minimum behavior.
4. Run and verify success.
5. Refactor.
6. Run full affected suite.
```

TDD is most valuable for:

- AuthService.
- TaskService.
- Repository behavior.
- Ownership rules.
- API HTTP contracts.

We do not need to artificially force test-first development for trivial configuration files.

---

# 26. Repository / Git Strategy

Use one Git repository.

Prefer small milestone-oriented commits.

Possible history:

```text
chore: scaffold backend and frontend
test: define task service behavior
feat: implement task application service
test: add sqlite repository integration coverage
feat: implement sqlite persistence
feat: implement authentication
feat: expose secured task API
feat: implement angular authentication flow
feat: implement angular task management
docs: add setup and genai documentation
chore: final validation and cleanup
```

Do not create fake historical commits.

Commit only actual development states.

---

# 27. Generative AI Documentation

Create:

```text
docs/GENAI.md
```

The assessment specifically asks to show:

- the prompt used;
- generated code or a representative sample;
- how AI suggestions were validated;
- what was corrected or improved;
- handling of edge cases;
- authentication;
- validation.

Record actual examples during development.

Do not invent corrections after the fact.

Suggested structure:

```text
# Generative AI Development Notes

## Tools Used
Codex / ChatGPT

## Initial Planning Prompt

...

## Milestone Prompts

...

## Example Generated Output

...

## Validation Performed

- dotnet build
- dotnet test
- Angular build
- Angular tests
- manual API validation
- browser validation

## AI Suggestions Rejected or Changed

### Example 1
AI suggestion:
Reason it was rejected:
Final implementation:

### Example 2
...

## Security Review

...

## Edge Cases Reviewed

...

## Lessons / Human Decisions

...
```

This is evidence of critical AI usage, not marketing material for AI.

---

# 28. Rules for Codex

Every implementation prompt should include these constraints.

### Scope

Implement **only the requested milestone**.

Do not begin future milestones.

### Architecture

Respect the established dependency direction.

Do not introduce new architectural layers without a demonstrated need.

### Forbidden dependencies

Do not use:

```text
Entity Framework
Dapper
MediatR
```

Do not introduce substitutes whose only purpose is recreating them.

### Dependencies

Do not add third-party packages when the framework or a small amount of code already solves the problem adequately.

If a new dependency is genuinely useful, explain why before introducing it.

### Testing

For business behavior designated for TDD:

1. add failing tests;
2. verify expected failure;
3. implement;
4. verify passing tests.

Run affected tests after changes.

### Validation

Before declaring a milestone complete:

```text
dotnet build
dotnet test
```

and, when frontend exists:

```text
npm/angular build
relevant frontend tests
```

must succeed.

### Security

Never:

- commit secrets;
- concatenate SQL inputs;
- store plaintext passwords;
- trust authorization data from frontend;
- disable authentication to simplify tests.

### Reporting

At completion of each milestone, report:

- files changed;
- behavior implemented;
- tests added;
- commands executed;
- test/build results;
- assumptions;
- deviations, if any;
- anything requiring human review.

Do not automatically continue to the next milestone.

---

# 29. Milestone Plan

## M0 — Repository Scaffold

Create:

- solution/repository structure;
- .NET projects;
- project references;
- Angular 22 project;
- test projects;
- `.gitignore`;
- basic README shell.

Verify:

```text
dotnet build
dotnet test
Angular build
```

No application functionality yet.

---

## M1 — Domain + Application Task Logic

Implement:

- `TaskItem`.
- `TaskStatus`.
- `ITaskRepository`.
- Task application models.
- `TaskService`.

Use TDD for:

- creation;
- validation;
- ownership;
- update;
- deletion.

No database.

No controllers.

---

## M2 — SQLite Persistence

Implement:

- schema initialization;
- SQLite connection configuration;
- `SqliteTaskRepository`;
- `SqliteUserRepository`;
- seed infrastructure.

Add repository integration tests.

Use explicit parameterized SQL.

No auth endpoints yet.

---

## M3 — Authentication

Implement with TDD where applicable:

- User model/contracts.
- `IUserRepository`.
- `IPasswordHasher`.
- `ITokenService`.
- `AuthService`.
- password hashing implementation;
- JWT implementation;
- registration;
- login;
- authenticated identity.

Add security-oriented tests.

---

## M4 — Web API

Implement controllers:

```text
/api/auth
/api/tasks
```

Configure:

- JWT Bearer.
- authorization.
- ProblemDetails/error handling.
- CORS.
- dependency injection.

Add API integration tests.

Prove user isolation.

At this point the backend should satisfy all functional backend requirements.

---

## M5 — Angular Authentication

Implement:

- login;
- registration;
- auth service;
- session token handling;
- interceptor;
- guard;
- logout;
- responsive authentication UI.

Validate against live backend.

---

## M6 — Angular Task CRUD

Implement:

- task list;
- empty state;
- create;
- edit;
- delete;
- status handling;
- due date;
- validation;
- loading/error feedback;
- responsive layout.

No unrelated features.

---

## M7 — Full-System Hardening

Run complete review.

Verify:

- build warnings;
- test failures;
- browser console;
- API errors;
- incorrect HTTP status codes;
- authorization boundaries;
- SQL parameters;
- secret exposure;
- nullable handling;
- duplicate usernames;
- malformed requests;
- nonexistent resources;
- responsive UI.

Fix only actual defects or clearly valuable improvements.

Do not add new product features.

---

## M8 — Submission Package

Complete:

- README.
- architecture description.
- setup.
- credentials.
- commands.
- test instructions.
- GenAI documentation.
- relevant assumptions.
- design decisions.

Perform clean-clone style validation.

Confirm repository is public and contains no secrets.

---

# 30. README Requirements

The final README should allow an evaluator unfamiliar with the repository to answer:

### What is this?

Short product/user-story description.

### How is it designed?

Brief architecture explanation.

### Why SQLite?

Short rationale.

### How do I run it?

Exact commands.

### What credentials can I use?

Demo credentials.

### How do I run tests?

Exact commands.

### What trade-offs were made?

Brief intentional omissions.

### How was GenAI used?

Link to:

```text
docs/GENAI.md
```

---

# 31. Acceptance Criteria

The project is functionally complete when all of these are true:

- New user can register.
- User can log in.
- Password is stored hashed.
- Successful login provides authenticated session/token.
- Anonymous user cannot access task endpoints.
- Authenticated user can create a task.
- User can list only their own tasks.
- User can retrieve one of their tasks.
- User can update one of their tasks.
- User can delete one of their tasks.
- User cannot read another user's task.
- User cannot modify another user's task.
- User cannot delete another user's task.
- Invalid input produces appropriate client errors.
- Duplicate username is handled.
- Missing resources return appropriate status.
- SQL uses parameters.
- Database initializes successfully.
- Demo user/data exist.
- Frontend exposes complete CRUD.
- Frontend is usable at desktop and narrow/mobile widths.
- Protected Angular route behaves correctly.
- Application-layer unit tests pass.
- SQLite integration tests pass.
- API integration tests pass.
- Backend build succeeds.
- Frontend build succeeds.
- Browser console has no known application errors.
- Setup instructions have been validated.
- Git repository contains no secrets.
- GENAI documentation contains real prompts and real review/correction examples.

---

# 32. Definition of Done

The project is ready for submission only when a clean reviewer can reasonably:

```text
clone
configure documented local secret
run backend
run frontend
log in with demo credentials
perform full CRUD
run tests
read architecture explanation
understand AI usage
```

without needing undocumented knowledge from the author.

---

# 33. Presentation Preparation

The assessment says the candidate must explain:

- user story;
- design decisions;
- architecture;
- functionality;

and then answer questions during a code review.

Prepare to explain, without memorized buzzwords:

### Why SQLite?

Because it satisfies the actual persistence requirements while keeping the application self-contained and the explicit SQL visible. Storage remains isolated behind application interfaces.

### Why Angular?

It is a framework the developer already knows and provides routing, DI, HTTP, forms and interceptors without additional architectural dependencies.

### Why no generic repository?

Because repositories model application persistence requirements. A generic repository would add abstraction without solving a requirement and would partially recreate ORM behavior.

### Why TaskService rather than CQRS handlers?

The current domain complexity does not justify CQRS. A focused application service provides clear boundaries with much less ceremony.

### Why JWT?

The application needs authenticated API requests from a separate SPA. JWT Bearer provides a simple explicit authentication mechanism suitable for the exercise.

### Why no refresh tokens?

The assessment requires login and authorization, not persistent production-grade session management. Adding refresh-token rotation would increase security-sensitive complexity without improving the evaluated use case.

### Why Clean Architecture?

To ensure business behavior depends on abstractions and remains independent from HTTP and SQLite.

---

# 34. Decision Principle

For every implementation decision ask:

> Does this make the required application safer, clearer, more testable, easier to run, or easier to explain?

If the answer is no, it probably does not belong in this assessment.

The target is not the most sophisticated architecture possible.

The target is a **complete, defensible implementation with deliberate technical decisions and no unnecessary machinery.**