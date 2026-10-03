# M4 Completion Report — Two-host Controller-based Web APIs

Publication note: this is a historical milestone record. Machine-specific executable paths in recorded commands are generalized as labeled placeholders; original scope, failures, successful retries, counts and evidence limits are preserved. Ignored launcher/runtime helpers are historical provenance, not reviewer prerequisites.

## Implemented

M4A shared composition, M4B authentication API and M4C task CRUD API are complete. Both independent executable hosts use the ASP.NET Core MVC controller pipeline and shared Domain/Application/Infrastructure behavior. No host-to-host reference, HTTP startup dependency, extra layer, Angular feature or commit was added. Existing uncommitted M0–M3/reconciliation work was preserved.

Shared presentation source composes absolute SQLite configuration, CORS, ProblemDetails and validated current identity. Infrastructure owns one JWT validation policy. Each host registers only its required application services/repositories. Existing persistence and business implementations were preserved.

## Files changed/created

- `src/backend/TaskManager.Api/Program.cs`, `.csproj`, `Tasks/TasksController.cs`, `Tasks/TaskRequest.cs`.
- `src/backend/TaskManager.Auth.Api/Program.cs`, `.csproj`, `Authentication/AuthController.cs`.
- `src/backend/SharedApi/ApiFoundation.cs`, `ApiExceptionHandler.cs`, `CurrentIdentity.cs` (linked into both hosts).
- `src/backend/TaskManager.Infrastructure/Authentication/JwtValidation.cs`.
- Both API test project files; `TaskManager.Auth.Api.Tests/AuthHttpTests.cs`; `TaskManager.Api.Tests/TaskHttpTests.cs`, `StartupTests.cs`, `FoundationHttpTests.cs`; linked `tests/SharedApiTests/ApiTestFactory.cs`, `InvalidTokens.cs`.
- README; PROJECT_DEFINITION, USER_STORY, DECISIONS (DEC-016), GENAI, REQUIREMENTS_TRACEABILITY; personal rehearsal material subsequently removed; this completion report.

These are M4 changes, not an inventory attributing the already-uncommitted M3/checkpoint work to M4. Historical M3/checkpoint completion reports were not edited. No frontend source/dependency files changed.

## API endpoints

| Host | Route | Success |
| --- | --- | --- |
| Auth.Api | POST /api/auth/register | 201 safe identity/token result |
| Auth.Api | POST /api/auth/login | 200 identity/token result |
| Auth.Api | GET /api/auth/public | Explicit anonymous 200 small message |
| Auth.Api | GET /api/auth/me | Explicit protected 200 safe identity |
| Task.Api | GET /api/tasks | Protected 200 owned collection, including [] |
| Task.Api | GET /api/tasks/{id} | Protected 200 owned task |
| Task.Api | POST /api/tasks | Protected 201 with GET-by-ID Location |
| Task.Api | PUT /api/tasks/{id} | Protected 200 updated representation |
| Task.Api | DELETE /api/tasks/{id} | Protected 204 |

Model binding/application validation produces 400; missing/invalid authentication and generic credential failure 401; duplicate usernames 409; missing/inaccessible tasks identical 404; unexpected errors safe 500 ProblemDetails. No response exposes passwords, password hashes, signing keys, SQL or stack traces. Registration does not invent a user resource Location.

Task JSON uses editable title, description, numeric status and required yyyy-MM-dd dueDate (the assessment's due_date calendar field). Status defaults to Pending. TaskRequest makes HTTP omission/null detectable without changing M1's DateOnly/business semantics. Ownership is absent from the request contract and derives only from validated claims; supplied body/query userId values cannot change it.

## Startup / shared SQLite behavior

Both hosts require `ConnectionStrings:TaskManager` / `ConnectionStrings__TaskManager`, with an absolute SQLite file Data Source, reject missing/empty/relative/in-memory host configuration and create the parent directory deliberately. No per-host fallback/resolution is used. Existing factory support for isolated caller/test strings is unchanged.

Both invoke the existing transactional/idempotent schema initializer. Task.Api starts without Auth.Api and never registers/invokes demo seeding. Auth.Api hashes the public demo password through the existing IPasswordHasher and invokes the existing seeder after initialization. Existing credentials, display casing, task edits and deletions are preserved on repeated/concurrent startup.

One configured HTTP(S) frontend CORS origin defaults to http://localhost:4200. HTTPS launch profiles/redirection remain available. README documents common inherited environment, local HTTP profiles, HTTPS ports, demo credentials and HTTP requests.

## JWT / cross-host behavior

Shared issuer, logical backend audience and external Base64 key (at least 32 bytes) are required. Existing M3 token issuance remains authoritative. Both hosts require HS256 signed/expiring JWTs, validate signature/issuer/audience/lifetime with zero skew and disable inbound remapping. Exactly one positive integer sub is required; malformed/missing/duplicate/nonpositive/overflow identities fail 401. No refresh tokens or authentication bypass were added.

Tests obtain a JWT through real Auth.Api HTTP registration/login, attach it to independent Task.Api HTTP requests, create a task under the authenticated owner and perform CRUD/isolation checks. No direct token-service call, fake principal or manual validation substitutes for this cross-host test. Invalid-token test construction is limited to rejection cases.

## Tests added

78 new cases: 23 Auth HTTP plus 55 Task/foundation/startup cases. They use real WebApplicationFactory/TestServer pipelines and isolated temporary absolute SQLite files with ephemeral keys and disabled pooling. Existing 65 Application unit and 48 real SQLite Infrastructure cases remain green (191 total).

| Required scenario | Automated proof |
| --- | --- |
| Task.Api starts first and creates schema without seeding | Yes — StartupTests.Task_host_starts_first_without_seed_then_auth_seeds_same_file_despite_different_roots |
| Auth.Api seeds after schema initialization | Yes — same startup test and new-file concurrent-host test; demo login succeeds over HTTP |
| Both hosts share one database despite different content roots | Yes — actual IWebHostEnvironment root assertions plus common demo/login/task data and no root-local db files |
| Auth.Api login JWT authenticates an HTTP request to Task.Api | Yes — TaskHttpTests.Auth_login_token_authenticates_task_CRUD_and_enforces_cross_user_isolation |
| Cross-user task isolation works through HTTP | Yes — collection/by-ID/update/delete; inaccessible resources return 404 |

Other coverage: all task verbs require authentication; malformed/model-binding input; omitted/null/unparseable dates; invalid title/status; invalid update preserves storage; Location; missing rows; generic credential failures; missing/duplicate/invalid subject; signature/issuer/audience/algorithm/expiry/not-before/required-expiration; configured/disallowed CORS origins in both hosts; safe unexpected 500 in both hosts; invalid database/JWT startup configuration; new-file concurrent initialization; repeated/concurrent startup preserves altered demo credentials, casing, edited and deleted tasks.

Initial tests and implementation were co-authored; no universal TDD chronology is claimed. Actual initial HTTP configuration failures and the valid-task 500 caused by record validation metadata placement are documented in GENAI.md, together with their fixes and observed passing suites.

## Validation performed

| Command | Exact result |
| --- | --- |
| `dotnet build TaskManager.sln --verbosity minimal` | Exit 0; all 9 projects; 0 warnings, 0 errors. Initial restricted restore could not read protected NuGet.Config; approved retry and final build passed. |
| `dotnet test TaskManager.sln --no-restore --verbosity minimal --diag TestResults/m4-final.log -- RunConfiguration.DotNetHostPath=<historical-launcher-path>` | Exit 0; Application 65, Infrastructure 48, Auth.Api 23, Task.Api 55; total 191 passed, 0 failed, 0 skipped. |
| `dotnet list TaskManager.sln package --include-transitive --no-restore` | Exit 0 after approved NuGet-config access retry; all 9 projects inspected; no Entity Framework, Dapper, Mediator/MediatR. |
| `npm run build` in Angular directory with bundled Node on PATH | Exit 0 after approved retry; Angular production scaffold bundle generated. Initial restricted attempt exited 1 without useful diagnostics. |
| `npm test -- --watch=false` with bundled Node on PATH | Exit 0; 1 test file, 2 tests passed. |
| `git diff --check` | Exit 0 after trimming introduced blank EOF lines; only Git line-ending notices. |
| Source/project/tree and traceability review | Shared inner dependencies preserved; feature controllers organized; no primary Minimal API routes, forbidden package, host-to-host dependency, secret or frontend feature introduced. |

The existing ignored local HostLauncher was inspected/reused unchanged. It corrects this machine's VSTest loopback endpoint and runs normal VSTest/xUnit assertions. Plain-runner reliability, browser/end-to-end behavior and a clean-clone functional review are not claimed. Diagnostics and temporary test data are not tracked application artifacts. Angular checks verify only its existing scaffold.

New required dependencies: Microsoft.AspNetCore.Authentication.JwtBearer 10.0.12 in each host (framework JWT middleware) and Microsoft.AspNetCore.Mvc.Testing 10.0.12 in each API test project (real HTTP testing). No new architectural project/layer or mapping/state framework.

## Documentation updated

README now describes runnable backend setup, matching common external configuration, ports, public demo credentials, API/status/wire contracts, tests and limitations. DEC-016 records shared composition and HTTP-contract choices. GENAI includes actual request excerpts, a clearly labeled representative complete prompt, actual generated REST output, validation and observed corrections. All 47 traceability IDs were reviewed; completed backend/HTTP/REST-output requirements have implementation/test evidence, and frontend/browser/final submission/presentation requirements remain incomplete. Canonical project/story and the personal rehearsal material available at M4 reflected M4 evidence. That personal material was subsequently removed; historical reports remain intact.

## Assumptions and deviations

Assumptions: canonical assessment transcription remains the operational source for the original PDF; each process receives the same external database/JWT values; one logical backend audience and one configured Angular development origin remain appropriate. JSON dueDate is the camelCase spelling of the assessment's due_date meaning, with numeric enum statuses.

No deviations from assessment, project/story, authorized M4 scope or existing architecture/security decisions. Validation uses the previously documented machine-specific runner workaround and approved sandbox retries. These do not weaken application behavior or replace assertions.

## Human review items

Review DEC-016's linked presentation source, the nullable/required date HTTP contract, numeric statuses, generic ProblemDetails and README setup before M5. Ensure both locally launched processes receive exactly the same path/key/issuer/audience. Standard VSTest portability on this machine still needs the existing environment issue resolved; final clean-clone review, Angular integration, browser validation and presentation belong to later milestones. No approval block remains.

Work stops after M4. No commit was created; M5 has not started.
