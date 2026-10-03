# Presentation and code-review guide

Use this as a 12–15 minute walkthrough, followed by the [five-minute demo](DEMO_CHECKLIST.md). Paths below are repository-relative. Start with behavior, then open code to show where each claim is enforced. This is preparation material, not evidence that the candidate has already delivered or rehearsed the presentation.

## 1. Problem and user story

**Explain:** “As a registered user, I want to securely manage my personal tasks so that I can keep track of the work I need to complete.” Registration/login and owned CRUD fulfill this story; tasks contain title, description, status and due date.

**Open:** `docs/USER_STORY.md`, `docs/ASSESSMENT_REQUIREMENTS.md`, `docs/REQUIREMENTS_TRACEABILITY.md`.

**Question:** Why this domain? **Answer:** It is small enough to finish and explain, and directly matches the assessment's GenAI task API example. Sharing/search/notifications are intentional non-goals.

## 2. Architecture overview

**Explain:** Angular SPA → two controller hosts → shared Application/Domain and Infrastructure implementations → one SQLite file. Dependencies point inward; runtime calls use application ports.

**Open:** `docs/ARCHITECTURE.md`, `TaskManager.sln`, backend `.csproj` references, `src/backend/SharedApi/ApiFoundation.cs`.

**Details:** Five production projects. SharedApi is linked presentation source, not another layer. Application and Domain have no HTTP/SQLite dependencies. Controllers translate requests and results; focused services perform use cases.

**Question:** Is this overengineered or microservices? **Answer:** The layers meet the assessment and isolate business/storage concerns. Two executable presentation hosts share the same inner layers/database; neither calls the other. There are no brokers or separate service databases.

## 3. Why two APIs exist

**Explain:** The assessment explicitly requires a SECOND API. TaskManager.Auth.Api owns register/login/public/me; TaskManager.Api owns protected task CRUD.

**Open:** `docs/DECISIONS.md` DEC-015/016, both `Program.cs`, `Authentication/AuthController.cs`, `Tasks/TasksController.cs`.

**Question:** Why not two controllers in one process? **Answer:** Human review identified that the original AI-assisted single-host plan omitted the explicit requirement. The pre-M4 reconciliation adopted two separate executable hosts. Historical reports preserve that correction instead of claiming the initial plan was right.

## 4. Authentication flow

**Explain:** Register/login → AuthService → supported hasher/user repository → JwtTokenService → JWT. Both hosts validate JWTs independently with the same external configuration.

**Open:** `Application/Authentication/AuthService.cs`, `Infrastructure/Authentication/FrameworkPasswordHasher.cs`, `JwtTokenService.cs`, `JwtValidation.cs`, `SharedApi/CurrentIdentity.cs`, `AuthController.cs` (backend paths).

**Details:** Trimmed 3–64-character case-insensitive username identity; preserved display casing. Password 8–128 UTF-16 characters, never trimmed; salted IdentityV3 PBKDF2-HMAC-SHA512, 210,000 iterations. Generic wrong-user/password 401; duplicate registration 409. JWT has positive integer `sub`, display `unique_name`, issuer/audience, nbf/exp. HS256, Base64 random key ≥32 bytes, default 15-minute lifetime (1–60), zero skew, unmapped claims. No secrets in source.

**Question:** Why does Auth.Api's token work in Task.Api? **Answer:** Identical issuer, logical audience, signing key and validation rules establish the same trust boundary; no call to Auth.Api is needed. Different settings reject tokens. HTTP tests obtain tokens from actual registration/login and send them to the independent task middleware.

**Question:** Does logout revoke a stolen JWT? **Answer:** No. Logout clears browser state; a captured valid token remains usable until expiry. `/me` returns validated claims, not a database revocation check. Refresh tokens/revocation are outside scope.

## 5. Persistence strategy

**Explain:** Direct Microsoft.Data.Sqlite, explicit SQL, parameters, mapping, disposal, constraints and transactions.

**Open:** `Infrastructure/Persistence/SqliteConnectionFactory.cs`, `SqliteDatabaseInitializer.cs`, `SqliteDemoSeeder.cs`, `Repositories/SqliteUserRepository.cs`, `SqliteTaskRepository.cs`.

**Details:** Users and Tasks in one external absolute file; every connection enforces foreign keys and the shared username collation. Schema/index initialization is transactional/idempotent. Both hosts initialize; Auth.Api alone seeds three tasks only for a newly inserted demo account. Existing credentials, edits and deletions survive restarts.

**Question:** Why SQLite and no ORM? **Answer:** No database server is needed for review; SQL remains visible and isolated behind application contracts. The assessment prohibits EF/Dapper/MediatR. This is not a general ORM/migration framework.

**Question:** Why custom collation? **Answer:** SQLite NOCASE covers ASCII only. `USERNAME_IDENTITY` shares Application's OrdinalIgnoreCase comparison, including trim behavior, and a unique index prevents races. Legacy collisions fail transactionally rather than merging accounts; external username writers must register the collation.

## 6. Task domain and application design

**Explain:** TaskItem owns invariants; TaskService coordinates owned use cases through ITaskRepository. No CQRS, generic repository or mediator is needed.

**Open:** `Domain/TaskItem.cs`, `TaskStatus.cs`, `Application/Tasks/TaskService.cs`, `TaskInput.cs`, `TaskResult.cs`, `ITaskRepository.cs`.

**Details:** Title trimmed/required/max 120; optional description trimmed/max 1000 and blank → null. Fixed numeric enum 0/1/2. New entity ID 0 becomes database-generated ID. DueDate is DateOnly; HTTP binding distinguishes missing/null input. Past dates are allowed. UI/JSON/SQLite preserve `yyyy-MM-dd` without timezone conversion.

**Question:** Why enum instead of status table, and why DateOnly? **Answer:** Three fixed states need no dynamic table. A task deadline is a calendar day, not an instant; timestamp conversion could shift it. JSON `dueDate` represents the assessment's `due_date`; string enum names/timestamps/impossible dates are rejected by the existing contract.

## 7. Ownership and server security

**Explain:** The server derives identity from successfully validated claims; ownership is never editable request data.

**Open:** `Tasks/TasksController.cs`, `TaskRequest.cs`, `SharedApi/CurrentIdentity.cs`, `Application/Tasks/TaskService.cs`, `Infrastructure/Persistence/Repositories/SqliteTaskRepository.cs`.

**Details:** Controller `[Authorize]`, exactly one positive subject, SQL `WHERE Id = @id AND UserId = @userId`, defensive service checks and affected-row handling. Owner-scoped list. Missing/inaccessible IDs share 404. Safe ProblemDetails and restricted one-origin CORS.

**Question:** Why not trust guards or client userId? **Answer:** Clients can send arbitrary HTTP requests. Guards are UX; middleware and owner-aware services/SQL enforce security for every request. Extra JSON/query owner fields cannot change it.

**Question:** Why 404 rather than 403? **Answer:** It avoids revealing another user's task existence and gives the same recovery behavior for absent/inaccessible records. Tests cover GET/PUT/DELETE and unchanged storage.

## 8. Angular and authentication integration

**Explain:** Standalone pages, Router, HttpClient, Reactive Forms and local signals, with separate API URLs and a single auth service.

**Open:** `src/frontend/task-manager-web/src/app/app.routes.ts`, `app.config.ts`, `core/api-config.ts`, `core/auth/auth.service.ts`, `auth.interceptor.ts`, `auth.guard.ts`, `auth/auth-page.ts`, `tasks/task.service.ts`, `task.models.ts`, `tasks-page.ts`/HTML/CSS.

**Details:** Token-only sessionStorage; same-tab reload calls `/me` before guarded navigation. Registration already returns a token. Exact origin/path interceptor scope prevents attachment to anonymous endpoints/assets/unrelated hosts. Protected 401/expiry clears matching state, old-token failures do not erase a newer session. Ten-second timeouts; failed saves keep drafts and failed deletes keep rows. Reload checks server state after uncertain writes; 404 directs cancel/reload. Mutations apply returned saved fields and prevent reload overlap.

**Question:** Why sessionStorage? **Answer:** Accepted assessment trade-off: shorter persistence than localStorage, but still accessible under XSS. No cryptographic trust is placed in client token parsing. Production cookies/BFF would require a deliberate broader design.

**Question:** Why no NgRx/separate form component? **Answer:** A focused tasks page and auth service cover this small state surface. Reactive Forms/signals provide the needed behavior without extra framework ceremony.

## 9. Testing and evidence

**Explain:** Different suites verify business behavior, real SQL, the HTTP pipeline, live integration and browser rendering.

**Open:** `tests/TaskManager.Application.Tests/TaskServiceTests.cs`, `AuthServiceTests.cs`, `tests/TaskManager.Infrastructure.Tests/SqlitePersistenceTests.cs`, `UsernameIdentityTests.cs`, `tests/TaskManager.Auth.Api.Tests/AuthHttpTests.cs`, `tests/TaskManager.Api.Tests/TaskHttpTests.cs`, `StartupTests.cs`, Angular `auth.spec.ts`, `tasks-page.spec.ts`, both `*.live.spec.ts`, `docs/M8_COMPLETION.md`.

**Details:** 194 backend cases; normal Angular 50 pass/two intentional live skips; enabling live probes yields 52 pass/no skips. Real HTTP/security harness checks 92 assertions. Browser evidence checks actual rendering/layout/console, beyond jsdom. WebApplicationFactory tests exercise middleware/controllers/real SQLite but use TestServer; live tests use running hosts.

**Question:** Was everything TDD? **Answer:** No. GENAI records observed M2 failing/passing tests and a failing M3 redaction regression. Some implementation/tests were co-authored, and tooling timeouts are not red behavior tests. Earlier reports document a local VSTest endpoint workaround without changed assertions/authentication. M8 ordinary fresh-clone `dotnet test` passed without it; it is not a reviewer dependency.

## 10. GenAI usage and critical review

**Explain:** Codex helped with scoped implementation, tests, SQL, UI and documentation. Human requirements/decisions and verification determined what was accepted.

**Open:** `docs/GENAI.md` REST API Prompt, Representative Generated Output, Validation, and Corrections and Human Review.

**Question:** Give a concrete correction. **Answer:** A generated record's default ToString exposed the password; an observed failing regression test led to redaction. Human review corrected the omitted second API. MVC record Required metadata initially caused 500; constructor-parameter metadata fixed it while retaining missing-date 400. M7 reproduced long-name mobile overflow and verified the small CSS correction.

**Question:** Are these original prompts? **Answer:** Actual excerpts are labeled; the consolidated complete REST-generation prompt is explicitly representative. The output sample is real controller code. No invented prompt history or universal test-first claim.

## 11. Live demo

**Explain:** Show seeded login, create/edit/status/calendar persistence, refresh, confirmed deletion and logout/guard behavior. Follow [DEMO_CHECKLIST.md](DEMO_CHECKLIST.md).

**Open:** README setup, the running SPA and TaskHttpTests cross-user test.

**Question:** How can I reproduce it after cloning? **Answer:** Restore dependencies, generate one external key/configuration for both hosts, start Auth.Api and Task.Api, npm start. No tracked DB/private file is needed. Existing seed accounts are preserved; use a fresh disposable path for deterministic data.

## 12. Decisions and trade-offs

**Explain:** Smallest defensible solution: focused services/ports, explicit SQL, two presentation hosts, numeric statuses/calendar dates, public demo data and short-lived browser session.

**Open:** `docs/DECISIONS.md` DEC-001/002/012–018, README limitations.

**Question:** Why no generic repository/unit of work? **Answer:** Specific task/user operations express actual needs. Transactions are used where necessary for schema/seeding; a generic abstraction adds no current value. Update/delete have owner-scoped SQL and affected-row checks, without concurrency-version features.

## 13. More production time

**Explain:** These are proposed future trade-offs, not implemented capabilities: deployment HTTPS/secret rotation, measured password cost, rate limiting, revocation/session design, CSRF policy if cookies are chosen, observability, versioned migrations/backups, concurrency versions and load-driven database choices. Browser automation across supported engines and pagination would depend on actual product/deployment needs.

**Open:** README intentional limitations, DEC-013/017/018.

**Question:** What is the biggest present limitation? **Answer:** This is a local assessment topology: public demo credentials and HTTP development profiles are deliberate, no deployment was built, no server revocation/refresh flow exists, and SQLite/last-write behavior are chosen for a small personal task workload. Do not claim production readiness.
