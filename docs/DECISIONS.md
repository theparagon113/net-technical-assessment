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

> Reconciliation note: DEC-015 refines the API presentation boundary to two executable hosts. The original decision text below is retained.

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

> Reconciliation note: DEC-015 supersedes the earlier single-host interpretation and defines compatible JWT validation across both hosts. JWT itself remains selected; historical text follows.

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

# DEC-010 — Task Model and Application Boundary (M1)

## Status

Accepted

## Decision

Use integer task/user identifiers, consistent with the conceptual schema. Positive IDs identify users and saved tasks; a task ID of zero represents an unsaved task. The repository assigns the saved identifier and returns a new task instance.

Represent the required due date with `DateOnly`, since tasks specify a calendar deadline without a time or time zone. Optional descriptions are trimmed and blank descriptions become null. Titles are trimmed, required, and limited to 120 characters; descriptions are limited to 1000 characters, following the project definition. Past dates and all defined statuses are allowed. Creation defaults to Pending but may explicitly select another valid status.

TaskItem is immutable and validates its invariants at construction. TaskService constructs a replacement on update, so invalid updates cannot partially mutate an existing task. Application input contains only editable fields, while results are immutable snapshots. Ownership comes exclusively from the current-user argument.

Repository reads and writes must be scoped to the owner. TaskService also checks returned ownership before exposing or modifying data. Missing and inaccessible tasks throw the same TaskNotFoundException with the same message. Invalid input uses framework ArgumentException subtypes; HTTP translation is deferred to the API milestone.

## Rationale and Trade-offs

These choices keep validation reusable without framework dependencies or duplicate rules, make ownership explicit, and avoid accidental mutation through repository references. DateOnly intentionally does not represent a timed deadline. Repository update/delete methods return whether an owned row was affected, allowing the service to handle a task disappearing between lookup and write without claiming success.

---

# DEC-011 — Minimal User Persistence and Deferred Demo Credentials (M2)

## Status

Accepted

## Context

M2 requires a user repository and seed infrastructure, while the milestone plan assigns user/authentication contracts and password hashing to M3. The explicit M2 prompt permits only the minimum persistence boundary and defers usable credentials.

## Decision

Add an immutable User containing Id, Username, and an opaque PasswordHash, plus IUserRepository insert and lookup operations. Zero denotes an unsaved user; storage assigns a positive integer ID. Trim usernames consistently on construction and lookup, retaining the documented schema's default case-sensitive SQLite uniqueness. Password hashing, verification, and any broader authentication normalization policy remain M3 work.

Provide a transactional demo seeder requiring an externally generated hash. Insert three representative tasks only when the demo user is newly created. On username conflict, preserve all existing records, including task edits/deletions and password hash. Supply no usable demo credentials in M2. Compose initialization and seed execution in later startup work.

## Rationale

This implements and tests the required storage without creating placeholder credentials, adding authentication prematurely, resetting an existing account, or duplicating seed tasks. A transaction makes partial seed failure safe to retry.

## Trade-offs

M2 alone does not provide a runnable seeded login. M3 must generate the demo hash with supported framework functionality. An existing account named demo receives no seed tasks; deleted demo tasks are intentionally not restored on subsequent startup.

---

# DEC-012 — Definitive Username Identity (M3)

## Status

Accepted; supersedes DEC-011's temporary case-sensitive username behavior only.

## Decision

Trim username boundaries and compare identity with the centralized `UsernamePolicy.Compare`, using `StringComparer.OrdinalIgnoreCase`. Preserve trimmed original casing in User and authentication results. Application usernames allow 3–64 UTF-16 characters and exclude control characters. No accent folding or Unicode canonical normalization is applied; non-case-equivalent spellings remain distinct identities.

Register the same comparison as SQLite `USERNAME_IDENTITY` on every factory-created connection. Add a unique index over `Username COLLATE USERNAME_IDENTITY`; lookup explicitly selects this collation too. Keep the existing case-sensitive unique constraint as a redundant compatibility constraint, avoiding a table rebuild and preserving user IDs, password hashes, task foreign keys, and display names.

Initialization installs the index transactionally. Existing case/trim-equivalent identities cause an explicit initialization failure and rollback without selecting a winning account, deleting data, or changing passwords. A developer must resolve those collisions deliberately before retrying. Repository inserts translate SQLite unique violations into `DuplicateUsernameException`, so availability checks and insert races share the application outcome. The demo seeder ignores conflicts on either username constraint and preserves existing accounts in every casing.

## Rationale and Trade-offs

SQLite's built-in NOCASE handles ASCII case only. A custom collation shares exactly one comparison with the application, supports Unicode ordinal casing, and avoids duplicated normalized columns or an ASCII-only username restriction. See [Microsoft's collation documentation](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/collation).

External database tools must register the collation for username writes; without it SQLite rejects the operation. Do not change this collation's semantics on an existing database without an explicit migration/reindex and collision review. Length validation is an authentication rule, while the repository remains capable of reading existing M2 storage fixtures.

---

# DEC-013 — Authentication Cryptography and Limits (M3)

## Status

Accepted; refines DEC-007/DEC-008 and completes DEC-011's hashing deferral.

## Decision

Wrap the supported framework `PasswordHasher<object>` in Infrastructure, using IdentityV3 with PBKDF2-HMAC-SHA512 and 210,000 iterations, independent random salts, and supported verification. Accept 8–128 UTF-16 password characters; reject blank input and preserve all password whitespace. Avoid arbitrary character-class requirements. Malformed legacy hash data fails verification. No full Identity persistence system is introduced.

Create access JWTs through Microsoft's `System.IdentityModel.Tokens.Jwt` package (8.19.2), rather than implementing signing/serialization manually. Infrastructure also references the existing ASP.NET shared framework for the password hasher/options; Application and Domain retain their original dependency boundaries.

Require explicit issuer/audience and a locally configured Base64 random signing key containing at least 32 bytes. Sign HS256 tokens containing only stable integer `sub`, display `unique_name`, issuer, audience, not-before, and expiry. Default lifetime is 15 minutes, configurable from 1–60 minutes. Reject invalid configuration at construction and copy the validated values. There are no refresh tokens. Validation middleware and options binding remain M4 work.

Authentication contracts redact password/access-token diagnostic strings. Invalid existing-user passwords and unknown usernames share one application exception/message; this does not promise constant-time lookup or equal timing for missing accounts. Registration duplicate errors intentionally report conflicts. Future deployment hardening such as rate limiting is outside this milestone.

## Rationale and Trade-offs

Framework hashing keeps crypto details outside business rules and provides supported adaptive salted verification. The explicit work factor is an assessment choice; deployed systems should measure hashing cost on their target hardware. Password hashes requesting framework rehash are accepted, but automatic hash upgrades are not introduced because the current repository has no update use case. JWT configuration stays outside Application/Domain; random test keys are ephemeral and no production/development signing secret is committed.

The intentionally public local demo account is `demo / Demo123!`. Callers hash the password and pass only the generated hash into the existing transactional seeder. Startup composition is deferred. Existing demo credentials and edited/deleted tasks are preserved.

---

# DEC-014 — Organize Source by Cohesive Module Within Clean Architecture Layers

## Status

Accepted

## Context

The solution uses lightweight Clean Architecture with separate Domain, Application, Infrastructure, and API projects.

As functionality grew through M1–M3, several projects accumulated unrelated source files directly at the project root. Although this does not affect compilation or architectural dependencies, it makes navigation harder and obscures the functional boundaries already present in the design.

Introducing additional architectural patterns only to solve file organization would add unnecessary complexity.

## Decision

Keep the existing Clean Architecture boundaries and organize source files inside each layer by cohesive feature or technical capability.

Application should primarily use feature-oriented modules, for example:

- Authentication
- Tasks
- Users when the user-related surface becomes large enough to justify its own module

Infrastructure should primarily use capability-oriented modules, for example:

- Authentication
- Persistence
- Persistence/Repositories

Domain may remain flat while it contains only a small number of closely related domain types.

Namespaces should follow the physical folder structure.

Do not create folders for trivial single types unless they represent a meaningful module boundary.

Do not introduce CQRS, MediatR, Unit of Work, generic repositories, vertical slices, or other architectural patterns solely to organize files.

## Rationale

This keeps the codebase easy to navigate and review while preserving the simplicity of the existing architecture.

Grouping related code makes functional boundaries visible without adding runtime complexity or unnecessary abstractions.

It also provides a consistent convention for future milestones so new API, authentication, task, and persistence code does not accumulate in flat project roots.

## Alternatives Considered

### Keep project roots flat

This is technically valid for very small projects but becomes harder to navigate as the number of related services, contracts, policies, exceptions, and persistence types grows.

### Organize only by technical type

Examples would include folders such as `Services`, `Interfaces`, `Models`, and `Exceptions`.

This was not selected as the primary Application structure because it separates files that belong to the same feature and makes understanding a use case require navigating several unrelated directories.

### Introduce a different architectural pattern

Vertical slices, CQRS, or additional layers could also impose structure.

They were not selected because the existing application complexity does not justify changing the architecture merely to improve file organization.

## Trade-offs

Some modules may initially contain only a few files.

The exact folder structure may evolve as the application grows, but changes should preserve cohesive grouping and avoid unnecessary nesting.

---

# DEC-015 — Use Separate Task and Authentication API Hosts

## Status

Accepted — required correction after M3, before HTTP endpoint implementation. Refines DEC-004's singular API presentation wording and supersedes the single-host interpretation in DEC-007; JWT and no-refresh-token decisions remain valid.

## Context

The original assessment explicitly requires a SECOND API. The earlier AI-assisted single-host plan was incomplete. Human review rechecked the original assessment and identified the omission before M4 controllers/endpoints were implemented. Historical M0–M3 reports remain unchanged; the correction does not invalidate M1–M3 task, persistence, or authentication logic.

## Decision

Use two executable ASP.NET Core hosts in the same outer Clean Architecture presentation layer:

- TaskManager.Api owns task CRUD HTTP endpoints and derives ownership from validated JWT claims.
- TaskManager.Auth.Api owns registration/login plus explicit authorized current-user and non-authorized public endpoints.

Both reuse the existing Domain/Application/Infrastructure projects and one Users/Tasks SQLite database; do not duplicate business logic or introduce microservice complexity. Use MVC/Web API controllers (AddControllers, MapControllers, [ApiController], ControllerBase, attribute routing), conservatively aligning with ASP.NET MVC/Web API wording. Angular remains the UI; no Razor views required.

Auth.Api issues tokens through existing shared services. Both hosts accept tokens consistently with the same issuer, logical backend audience, externally supplied Base64 signing key, HS256 algorithm and validation rules (signature, issuer, audience, expiry, required signed/expiring tokens; planned zero clock skew and unmapped claims). No refresh tokens. Full binding/middleware remains M4A.

M4 requires both composition roots to reject missing/relative SQLite file Data Source configuration and consume one externally supplied absolute path, independent of content/working roots. The factory currently has no host binding and remains usable by isolated SQLite tests. Both hosts may initialize the transactional/idempotent schema. Auth.Api alone seeds after initialization, hashing the existing public demo password through IPasswordHasher. Existing usernames/credentials and task edits/deletions remain preserved. Evaluators start Auth.Api first for demo population. Test repeated/concurrent host initialization in M4A.

Angular will configure two base URLs. Auth.Api.Tests follows the existing dedicated host-test convention; both HTTP suites and cross-host token acceptance are M4 work.

## Rationale

This satisfies the explicit assessment requirement with the smallest structural correction. Canonical requirements and living traceability prevent architecture choices from silently overriding the assessment. A required absolute database path avoids per-host file creation from differing launch directories without redesigning persistence.

## Alternatives Considered

Two controllers in one host would retain the incomplete interpretation and were rejected by the developer's checkpoint instructions. Separate business layers/databases/microservice infrastructure add complexity without a requirement.

## Trade-offs

Evaluators eventually run two processes and supply identical database/JWT configuration to both; frontend CORS/configuration and integration tests must cover both hosts. Scaffold existence alone does not satisfy auth endpoints. The checkpoint adds structure/documentation only; M4 adds composition and HTTP behavior.

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

# DEC-016 — Shared M4 Host Composition and HTTP Contracts

Accepted in M4; implements DEC-015 without changing the two-host architecture. Historical M0–M3/checkpoint reports remain unchanged.

Compile the small `src/backend/SharedApi` presentation source set into both executable hosts using linked Compile items. It owns common composition, middleware, safe ProblemDetails translation and validated identity extraction. No extra runtime project/layer or host-to-host reference is introduced. Infrastructure/Authentication/JwtValidation owns the compatible JWT validation parameters and reuses M3 issuance configuration validation. Only Auth.Api registers authentication services/seeding; only Task.Api registers task services/repositories.

Both hosts fail fast for invalid database/JWT configuration before creating storage, require an absolute file Data Source, deliberately create its parent directory and initialize schema. Auth.Api alone hashes/seeds demo data. One configured HTTP(S) CORS origin defaults to Angular's localhost:4200. Both hosts use unmapped claims, HS256, required signed/expiring tokens, zero skew, and one positive integer subject. Invalid/missing/duplicate subjects fail authentication with generic 401.

Reuse safe Application AuthInput/AuthResult and TaskResult contracts. A thin API TaskRequest uses nullable/required dueDate to distinguish omitted/null HTTP input from a supplied DateOnly value, without changing M1 business date semantics. JSON uses camelCase (`dueDate` represents the assessment's `due_date`) and numeric statuses, 0/1/2. Task input has no UserId; unknown JSON/query ownership values cannot override claims. PUT returns 200 updated representation, DELETE 204, POST 201 with GET-by-ID Location. Registration returns 201 without inventing a user resource. ArgumentException maps to safe generic 400; missing/inaccessible tasks share 404; credentials share generic 401; duplicates 409; unexpected errors safe 500.

Add Microsoft.AspNetCore.Authentication.JwtBearer 10.0.12 to the two hosts and Microsoft.AspNetCore.Mvc.Testing 10.0.12 to the two HTTP test projects, supporting required framework middleware and real HTTP testing. Cross-host tests reference both executable assemblies, exercise real registration/login/JWT/task requests, and use isolated absolute temporary databases and ephemeral keys. Test support is linked source, with early host configuration supplied before fail-fast composition. No ORM/mediator or production behavior bypass is added.

Trade-off: linked presentation source must be included by both host project files; this is visible in the solution and avoids either duplicate policy or a new architectural project for three small common files. The two processes still require matching external configuration; no cross-process configuration negotiation is introduced. HTTP integration tests use TestServer, not browser/end-to-end validation.

# DEC-017 — M5 Browser Session and Two-API Integration

Accepted in M5; implements the existing assessment sessionStorage trade-off.

Keep two public base URLs in a small injectable Angular configuration. Use one signal-based AuthService and a shared login/registration form component. Both existing backend operations return safe identity plus JWT, so both establish the returned session directly; registration makes no hidden login call. Persist only the JWT in sessionStorage; restore safe identity via `/me` before guards allow protected navigation. JWT payload parsing checks expiration for UX only; the backend verifies cryptography and authorization. No refresh tokens or duplicate global store.

Scope the functional interceptor to exact configured origins and `/api/auth/me` or `/api/tasks` path boundaries. Login/register/public/assets/third-party requests receive no JWT. Protected 401s invalidate the matching session once, while late failures from an older token cannot clear a newer session. Expiration clears state too; logout's returned navigation promise lets callers await route completion. Requests time out after ten seconds. Restoration failures clear state conservatively. Keep the task-host live probe in an opt-in test, without production task UI.

Trade-offs: JavaScript/XSS can access sessionStorage; its lifetime is shorter than localStorage. Production could use secure HttpOnly cookies/BFF. Forms supply UX validation matching current .NET UTF-16 length/whitespace rules, while backend validation and ownership remain authoritative. One reusable component is sufficient for the two closely related forms, without a UI framework or general error infrastructure.

# DEC-018 — M6 Task UI Contract and Persisted State

Accepted in M6. Keep the task feature in one cohesive Angular tasks module: typed models/mapping/UX validators, a small HttpClient service, and a standalone page with its template/CSS. Signals hold local list/loading/form/error state; Reactive Forms hold editable fields. No shared store, new dependency or backend contract change.

TaskService consumes the centralized taskApiBaseUrl exclusively and reuses M5's Bearer/401 handling. Requests have no ownership fields; returned identifiers are never exposed as editable UI. Centralize 0 Pending / 1 In progress / 2 Completed in an explicit numeric enum and label table. .NET DateOnly serializes yyyy-MM-dd, matching the HTML date input calendar value. Validate real dates and pass the string unchanged through named API/input transformations; never construct a JavaScript Date or convert UTC. Past dates remain allowed.

Apply server POST/PUT results only after success, and remove a row only after successful DELETE. Use inline delete confirmation. Preserve failed drafts/rows and render safe contextual errors. Requests time out at ten seconds, matching M5's UX duration; after an unconfirmed write the user can reload to inspect saved state before retrying. Disable duplicate submissions and prevent list reload/mutation overlap so a late list response cannot overwrite a saved result.

Trade-offs: the date display uses an unambiguous ISO calendar string rather than localized DatePipe formatting. Local UI state can become stale after another tab changes a task; 404 feedback directs the user to cancel/reload. No realtime synchronization or concurrency-version infrastructure is warranted for M6.
