# Technical Assessment — Project Definition & Execution Plan

**Deadline:** October 4, 2026 — 10:00  
**Repository:** Single public GitHub repository  
**Project:** Personal Task Manager  
**Primary objective:** Deliver a small, complete, secure, tested full-stack application that satisfies the assessment requirements and can be confidently explained during a live code review.

---

# 1. Source of Truth

Current status: M0–M6 and the post-M3 checkpoint are complete. Both controller-based hosts compose shared services, require one absolute SQLite file, initialize schema, validate compatible JWTs, and expose the specified auth/task endpoints. Auth.Api owns preserved demo seeding. Angular authentication now integrates both hosts, with session restoration, scoped Bearer forwarding, guards and logout. M6 adds responsive task list/forms/confirmed deletion with numeric status and calendar-date mappings. Final full-system checks (M7) and submission/presentation (M8) remain. See M4_COMPLETION.md, M5_COMPLETION.md, M6_COMPLETION.md and REQUIREMENTS_TRACEABILITY.md.

Implementation decisions should follow this priority:

1. Original assessment requirements, canonically transcribed in `docs/ASSESSMENT_REQUIREMENTS.md`.
2. This project definition.
3. `docs/USER_STORY.md`.
4. `docs/DECISIONS.md`.
5. The current milestone specification.
6. Implementation convenience.

If a coding agent proposes something that conflicts with a higher level, the higher-level requirement wins.

The external PDF remains authoritative. REQUIREMENTS_TRACEABILITY.md records status without overriding requirements; check and update applicable rows before milestone completion. The reconciliation checkpoint corrects the earlier single-host omission without rewriting M0–M3 history. Full assessment requirements and evaluation criteria are in ASSESSMENT_REQUIREMENTS.md.

The assessment explicitly requires:

- .NET / C# backend.
- ASP.NET MVC / Web API.
- A database or data store.
- Clean Architecture principles.
- TDD methodology.
- CRUD functionality.
- A SECOND API for user creation, login, authorized behavior, and non-authorized behavior.
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
- Anonymous registration and login routes on TaskManager.Auth.Api.
- Explicit anonymous public and protected current-user endpoints on TaskManager.Auth.Api.
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

Use a lightweight Clean Architecture with two separate executable ASP.NET Core API hosts in the same outer presentation layer:

```text
TaskManager.Domain
        ↑
TaskManager.Application
        ↑
TaskManager.Infrastructure
        ↑
        ├── TaskManager.Api       (task CRUD)
        └── TaskManager.Auth.Api  (registration/login/public/current user)

Angular
   ├── auth API base URL → TaskManager.Auth.Api → issues JWT
   └── task API base URL → TaskManager.Api      → validates JWT
                          TaskManager.Auth.Api also validates JWT for /me
```

Dependencies flow inward: Application depends on Domain; Infrastructure depends on Application and Domain; both API hosts compose shared services/repositories and translate HTTP. Domain has no application-specific dependencies. Business rules remain independent of HTTP and SQLite.

TaskManager.Api owns task CRUD only, derives owner identity from validated token claims, and never trusts frontend UserId. TaskManager.Auth.Api owns registration, login, an explicit anonymous/public endpoint, and a protected endpoint returning safe current-user information. Do not duplicate Domain, Application, Infrastructure, or business/token-generation logic. Both hosts use one shared SQLite database; no microservice infrastructure, separate databases, brokers, or distributed transactions.

Both use ASP.NET Core's MVC/Web API controller pipeline: `AddControllers()`, `MapControllers()`, `[ApiController]`, `ControllerBase`, attribute routing. This conservatively aligns with "ASP.NET MVC, Web API" while Angular remains the UI. No Minimal APIs for primary assessment endpoints; no Razor/server-rendered UI is required. DEC-015 records the correction to DEC-004/007.

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
│   │   ├── TaskManager.Api/
│   │   └── TaskManager.Auth.Api/
│   │
│   └── frontend/
│       └── task-manager-web/
│
├── tests/
│   ├── TaskManager.Application.Tests/
│   ├── TaskManager.Infrastructure.Tests/
│   ├── TaskManager.Api.Tests/
│   └── TaskManager.Auth.Api.Tests/
│
├── docs/
│   ├── ASSESSMENT_REQUIREMENTS.md
│   ├── REQUIREMENTS_TRACEABILITY.md
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

Database initialization is transactional and idempotent; M4A startup composition is implemented. Both hosts ensure schema initialization. Auth.Api alone hashes the demo password and invokes SqliteDemoSeeder after initialization. Reruns preserve existing credentials and task edits/deletions, including existing demo usernames in any casing; deleted demo tasks are not restored.

Both M4 composition roots read `ConnectionStrings:TaskManager` (`ConnectionStrings__TaskManager`) and reject missing or relative file Data Source values. Require an externally supplied absolute path to the same SQLite file, never resolve against either host's working/content root and never silently fall back to a local relative file. Create its containing directory first. The existing factory accepts caller-supplied strings; both hosts now bind the validated common connection string. Isolated caller/test in-memory configurations remain supported. No persistence redesign was needed for M4. M4 tests prove both hosts use the same file with different content roots and repeated/concurrent startup initialization. Start Auth.Api first for evaluator demo seeding; Task.Api can start first but will not seed the demo account.

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

# 12. Authentication Design (implemented HTTP behavior: TaskManager.Auth.Api)

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

Register returns 201 with safe identity/token result; validation returns 400 and duplicate username returns deterministic 409. Never return `PasswordHash`.

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

## Explicit Public Endpoint

`GET /api/auth/public` returns 200 without JWT and exposes only safe public API information. Registration and login are also anonymous. This explicit endpoint fulfills non-authorized behavior separately from the protected current-user endpoint.

## Current User

```text
GET /api/auth/me
```

Requires authentication.

Returns safe user ID/display username derived from the validated identity: 200 with valid JWT, 401 without a valid JWT. Never return passwords, hashes, or a token from this endpoint.

---

## JWT

JWT contains only information required to identify the user, such as:

```text
sub
unique_name
```

Configuration validates:

- signing key;
- issuer;
- audience;
- lifetime;
- signature.

Preserve M3/DEC-013: HS256; default lifetime 15 minutes, configurable 1–60; Base64 random signing key of at least 32 bytes. Auth.Api issues JWT through AuthService/JwtTokenService. Both hosts validate the same issuer, one logical backend audience, signing key, HS256 algorithm, signature and expiry. Require signed tokens/expiration; reject wrong key/issuer/audience/algorithm and expired tokens. Set matching zero clock skew and disable inbound claim remapping so positive integer `sub` is extracted consistently. Reject invalid identity claims rather than trusting request ownership fields. Validation policy is shared in Infrastructure/Authentication/JwtValidation; do not duplicate token generation or business rules.

Supply secrets locally via a common inherited environment (README preparation example) or configure identical secrets for both hosts. Never commit a signing key. Lifetime/configuration validation must fail clearly at startup in M4A. M4 now configures and tests this JWT pipeline in both hosts.

No refresh-token implementation.

---

# 13. Task API (implemented HTTP behavior: TaskManager.Api)

All task endpoints require authentication; missing/invalid JWT returns 401, invalid input 400, and missing/inaccessible tasks 404. Updates consistently return 200 with the updated representation; deletes return 204. POST returns 201 with Location/CreatedAtAction where practical. Controllers only translate requests/results/errors, preserving inner business rules.

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

Configure TWO backend base URLs: auth API for registration/login/public/current-user calls; task API for task CRUD. Scope the interceptor to these configured backend origins/paths. Both hosts allow the documented Angular origin through CORS in M4A; frontend integration is implemented in M5/M6.

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

Use real ASP.NET Core HTTP-pipeline integration tests (`WebApplicationFactory` or equivalent) in TaskManager.Api.Tests and TaskManager.Auth.Api.Tests. Keep Application unit tests and real SQLite Infrastructure tests. Thin controllers do not need redundant direct unit tests to inflate counts.

Task API: all CRUD verbs, collection/by-ID success, validation/status codes, unauthenticated rejection, authenticated success, user isolation on reads/updates/deletes, identical missing/inaccessible 404, POST 201 Location, PUT 200, DELETE 204.

Auth API: registration 201, duplicate 409 (including casing/race mapping), login 200, generic invalid-credentials 401, public anonymous 200, protected /me 401 without/with invalid JWT and 200 with valid JWT; wrong key/issuer/audience/algorithm and expired tokens; model-binding/validation 400. Never expose password hashes.

Cross-host: a token obtained over HTTP from TaskManager.Auth.Api is accepted by TaskManager.Api under shared test issuer/audience/key. Test shared database path enforcement with distinct host content roots and initialization/seed preservation on reruns. Use isolated databases/ephemeral keys. Add behavior tests first where practical and record actual red/green evidence.

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

## Reconciliation checkpoint — between M3 and M4

Human requirement review found the initial AI-assisted plan omitted the explicit SECOND API requirement. Correct the canonical sources/traceability/architecture and add Auth.Api/Auth.Api.Tests scaffolds; no controllers, JWT middleware, composition, or frontend features. Preserve historical M0–M3 reports. Existing business/persistence/auth services remain valid; M0's host count was incomplete and is corrected here. See RECONCILIATION_COMPLETION.md.

## M4 — Two-host controller-based Web APIs

### M4A — Shared API foundation / composition

- Controller-based setup in both hosts, DI for existing services/repositories.
- Required identical absolute shared SQLite file configuration, initialization in both hosts, demo seeding only in Auth.Api.
- Compatible JWT validation in both hosts, one backend audience, shared issuer/key/rules, claim extraction policy.
- ProblemDetails/error mapping, Angular-origin CORS, configuration/startup validation.
- Foundation tests, including differing content roots, shared database and idempotent startup.

### M4B — Authentication API (TaskManager.Auth.Api)

- POST /api/auth/register; POST /api/auth/login.
- GET /api/auth/public (explicit anonymous); GET /api/auth/me (explicit protected).
- 201/409/200/401/400 semantics, generic login failures, duplicate mapping, safe results.
- Real HTTP-pipeline tests in TaskManager.Auth.Api.Tests; token validation cases.

### M4C — Task CRUD API (TaskManager.Api)

- GET collection; GET by ID; POST; PUT (200 updated result); DELETE.
- JWT authentication, validated positive integer current-user claim, owner-aware service calls.
- Validation/error mapping; identical missing/inaccessible 404; 201 Location and 204 deletion.
- Real HTTP-pipeline tests in TaskManager.Api.Tests, all CRUD verbs and cross-user isolation.
- Cross-host test: Auth.Api-issued JWT accepted by TaskManager.Api using shared test configuration.
- Record actual REST API prompt/output and HTTP validation evidence in GENAI.md; do not fabricate TDD chronology.

M4A/B/C are implemented and tested; see M4_COMPLETION.md for results and REQUIREMENTS_TRACEABILITY.md for the updated requirement audit. M5 results are recorded separately in M5_COMPLETION.md.

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

Configure separate auth/task API base URLs; authentication calls target Auth.Api. Validate against both live backend hosts, including token forwarding to Task.Api.

Implemented in M5: login/registration both consume M4's identity/token result, sessionStorage JWT only, authoritative `/me` restoration, guarded `/tasks` placeholder and logout, restricted interceptor and safe form errors. Automated unit/router and opt-in live two-host tests plus browser registration/login/refresh/logout evidence are recorded in M5_COMPLETION.md. No M6 task UI was added.

---

## M6 — Angular Task CRUD

Task service uses the task API base URL; auth/session service uses the auth API base URL.

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

Implemented in M6: guarded task list, create/edit forms, inline confirmed deletion, centralized numeric status labels and validated DateOnly calendar strings. Safe failures preserve drafts/rows; timeouts and reload recovery are explicit. Automated mapping/component tests, both opt-in real-host probes and browser CRUD/session/mobile/desktop evidence are recorded in M6_COMPLETION.md. No backend changes or dependencies were required.

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
- complete requirements traceability and presentation/rehearsal, including the user story, two APIs, testing, GenAI prompt/output/corrections, and live functionality demo.
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

- Two separate API hosts expose their assigned controller endpoints, including explicit public and protected auth behavior.
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
run both backend API hosts against one database and shared JWT configuration
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

### Why two APIs?

The assessment explicitly requires a second API. Two executable hosts share the same inner layers/database; this is a presentation-layer split. Human review corrected the initial single-host omission before endpoints were implemented.

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
