# Generative AI Development Notes

## Tools Used

Codex, operating on the repository in the desktop app. This record covers observed M2 and M3 work; it does not reconstruct earlier milestones.

## M2 Prompt — Actual Excerpts

> M0 and M1 are complete. Implement **M2 only: SQLite Persistence**.

> Implement the SQLite persistence layer and repository integration tests using `Microsoft.Data.Sqlite` and explicit parameterized SQL.

> Do not change the repository contract merely to make SQLite implementation easier.

> If safely seeding the final demo user would require password-hashing behavior that does not yet exist, create the deterministic/idempotent seed mechanism and database support now, and clearly report the final credential/hash population as deferred to M3.

The full supplied prompt additionally required real SQLite tests, ownership predicates, invariant DateOnly mapping, cancellation propagation, minimal user storage contracts, and no authentication/API/frontend implementation.

## Representative Generated Output

The repository update uses explicit owner predicates and affected-row results:

```csharp
command.CommandText = """
    UPDATE Tasks SET Title = @title, Description = @description,
        Status = @status, DueDate = @dueDate
    WHERE Id = @id AND UserId = @userId;
    """;
AddTaskParameters(command, task);
command.Parameters.AddWithValue("@id", task.Id);
return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
```

## Observed Validation and TDD Evidence

- Tests were written before repository SQL, using temporary NotImplementedException methods.
- The first test command was blocked by sandbox access to the user NuGet configuration. The elevated retry aborted on test-host startup timeout; another retry with a 180-second timeout also aborted. Neither counted as a red test result.
- A diagnostic run reached the tests after schema initialization had been implemented: 22 failed against unimplemented repository/seeder methods.
- After implementing user SQL: 2 passed, 20 failed against remaining task/seeder methods.
- After implementing task SQL and seeding: all 22 passed.
- Four additional cases were added after implementation to verify transaction rollback and rejection of absent hash input. These are additional verification, not claimed as test-first development.
- Unchanged M1 Application tests passed: 45 cases.

Final checks completed:

- `dotnet build`: success, zero warnings and zero errors.
- `dotnet test`: success, 45 Application plus 26 Infrastructure tests (71 total), no failures/skips. The API test scaffold contains no tests, as expected before M4.
- `npm run build`: the system Node v24.14.1 was rejected by Angular. With bundled Node v24.19.0 on PATH, the sandboxed build exited without diagnostics; an elevated retry succeeded. No frontend source or dependency changes were needed.
- `npm test -- --watch=false` with bundled Node v24.19.0: two tests passed in one test file.
- `git diff --check`: passed.

Targeted commands used during the cycle were `dotnet test tests/TaskManager.Infrastructure.Tests/TaskManager.Infrastructure.Tests.csproj --verbosity minimal`, subsequent runs with `--no-restore`, and the diagnostic run with `--no-build --diag TestResults/m2-infrastructure-runner.log`. M1 was checked independently with `dotnet test tests/TaskManager.Application.Tests/TaskManager.Application.Tests.csproj --no-restore --diag TestResults/m2-runner.log --verbosity minimal`. Startup timeout retries and permission failures are recorded above as tooling failures, not test evidence.

## Review and Scope Decisions

- Preserved the existing uncommitted M1 work, TaskItem invariants, and ITaskRepository signature/semantics.
- Selected file-per-test isolation with pooling disabled so real independent repository connections share a persistent database during each test and release handles before file deletion.
- Required a caller-supplied hash for seeding; test fixtures are opaque storage data, not fabricated usable credential hashes. Framework hashing and demo credential population remain M3.
- During review, the existing gitignore had no SQLite exclusions. Added local `.db` and sidecar exclusions because persisted data can include password hashes.
- No human corrections or rejected AI proposals have been reported during this M2 interaction. These scope choices are not presented as retrospective correction evidence.

## Security and Edge Cases Reviewed

Real SQLite coverage checks parameterized strings containing SQL syntax, unique usernames, missing rows, null descriptions, leap-day dates under ar-SA culture, every status, generated IDs, ownership on reads/writes, foreign keys despite a disabling input connection string, cancellation, repeatable schema/seed execution, existing demo usernames, preserved hash/task edits/deletions, and rollback after a simulated second-task seed failure.

Authentication, HTTP responses, browser behavior, and password verification are outside M2 and have not been validated here.

## M3 Prompt — Actual Excerpts

> Implement **M3 only: authentication application/infrastructure services**.

> M3 **must replace that temporary username identity behavior with a consistent case-insensitive authentication policy**.

> The database constraint/index must prevent case variants from being inserted even if an application-level availability check is bypassed or races.

> M3 ends at authentication application/infrastructure behavior.

The supplied prompt also required supported password hashing, small immutable authentication contracts, JWT configuration and signature/expiration tests, usable demo hashes, real SQLite identity tests, security review, documentation, complete validation, preservation of previous milestones, and no commit.

## M3 Representative Generated Output

```csharp
public static int Compare(string? left, string? right) =>
    StringComparer.OrdinalIgnoreCase.Compare(left?.Trim(), right?.Trim());
```

The factory registers that comparison on every connection. The initializer adds the constraint:

```sql
CREATE UNIQUE INDEX IF NOT EXISTS IX_Users_UsernameIdentity
    ON Users (Username COLLATE USERNAME_IDENTITY);
```

## M3 Observed Testing and Corrections

- AuthService implementation and its initial tests were authored together; this is not claimed as test-first development.
- Four SQLite identity tests were written before persistence changes. Initial executions never reached assertions because the test host could not connect; those attempts are tooling failures, not verified red tests. Persistence implementation then proceeded, and the cases passed during full validation.
- Security review identified that the AI-generated positional AuthInput record's default ToString included the password. A new regression test ran and failed with `secret-password!` visible in the diagnostic string. Overriding ToString removed this exposure; the targeted test passed. AccessToken and AuthResult diagnostic strings were also redacted and covered. This is actual observed red/green evidence and an actual correction of generated output, rather than retrospective TDD history.
- Replaced an unnecessarily brittle exact JWT claim count assertion with absence-of-password-claims coverage while retaining signature, identity, issuer, audience, and expiry validation.
- Updated one M2 duplicate-insert test to expect the new documented application exception, retaining its original preservation assertion. New raw SQL tests still assert SQLite unique error 2067; the persistence guarantee was strengthened, not weakened.
- Added a deterministic real SQLite race: a competing lowercase account is inserted after availability lookup, then the original insert raises the same DuplicateUsernameException without calling token creation.

## M3 Runner and Validation Evidence

NuGet commands initially failed to read the user's protected configuration; elevated retries succeeded. The system's loopback listener redirection caused repeated test-host startup timeouts. Diagnostic logs showed VSTest listening on a LAN address while launching testhost with a 127.0.0.1 endpoint. A standalone TcpListener check reproduced this behavior, including outside the sandbox. Switching to the installed VSTest 17 runner did not fix it.

A temporary ignored `TestResults/HostLauncher` console program measures the actual listener address, replaces only the testhost endpoint argument, and launches the normal dotnet test host. It changes no test code, discovery, assertions, application logic, dependencies, or machine settings. An initial incorrectly cased `DotnetHostPath` setting was rejected; the supported spelling `DotNetHostPath` worked. The workaround is local tooling and is not part of tracked application source.

Successful complete-suite command:

```powershell
dotnet test --no-restore --verbosity minimal --diag TestResults/m3-final.log -- RunConfiguration.DotNetHostPath=C:/Maethrillian/NET-TechnicalAssessment/TestResults/HostLauncher/bin/Debug/net10.0/HostLauncher.exe
```

The complete-suite results and final build/dependency checks are recorded in [M3 completion report](M3_COMPLETION.md). No API/browser authentication behavior is claimed; that remains M4+.

## Requirements Reconciliation — Actual Human Review and Correction

The developer's checkpoint prompt states:

> a human review of the original assessment found that the initial execution plan omitted an explicit requirement: the assessment requires a SECOND API for authentication-related functionality.

> Do not implement M4 endpoint functionality in this checkpoint.

The initial AI-assisted plan treated authentication as part of one API host. Human review rechecked the original assessment and found the explicit second-API requirement missing. This checkpoint corrects architecture/roadmap before HTTP endpoints exist, adds TaskManager.Auth.Api and its dedicated test scaffold, and adds canonical requirements and living traceability to prevent recurrence. DEC-015 records the correction; historical reports remain unchanged. This is concrete evidence of critical human review of generated planning, not flawless AI output or blind acceptance.

The audit also replaced the relative SQLite setup example with a planned required common absolute path, assigned Auth.Api demo-seeding ownership, documented compatible JWT validation and two frontend base URLs, and explicitly planned public/protected auth endpoints and cross-host tests. These are documentation/scaffold corrections; startup and endpoints remain M4.

## Final GenAI Deliverable Readiness

| Required item | Actual evidence | Remaining work |
| --- | --- | --- |
| Task REST API generation prompt | Actual M2/M3 prompt excerpts and checkpoint excerpt above | PENDING M4: capture the actual REST task API prompt covering create/read/update/delete, title/description/status/due_date and user ownership. No historical API prompt is invented. |
| Representative REST API output | Actual SQL and identity samples above | PENDING M4: HTTP API sample does not yet exist. Preserve a real generated controller/service excerpt and explain its reviewed behavior. |
| Validation of suggestions | Observed builds/tests/security review in M2/M3 and checkpoint report | M4 HTTP tests, M5–M7 browser/manual evidence. |
| Corrections/improvements | M3 diagnostic redaction regression; this human second-API planning correction | Explain both concretely in presentation; add further actual corrections only if observed. |
| Edge cases | SQL-like text, nullable fields, dates/culture, ownership, duplicate races, Unicode/collisions, seed rollback/preservation | M4 malformed binding, required dates/statuses, inaccessible resources, invalid tokens; M5/M6 UI cases. |
| Authentication handling | AuthService, supported hasher, JWT generation/signature tests; DEC-013/015 | M4 shared issuer/audience/key validation in both hosts; HTTP cross-host proof. No refresh tokens. |
| Validation handling | Domain invariants and Application policies; boundary tests | M4 ProblemDetails/status mapping; M5/M6 form feedback. |

No new functional test-first chronology is claimed for this documentation/scaffold checkpoint. Build/test results and any runner limitations are recorded in RECONCILIATION_COMPLETION.md. Final presentation evidence remains incomplete until M4–M8.

## M4 Prompt — Actual Request Excerpts

> Implement **M4 only: Two-host controller-based Web APIs**.

> Use the existing `TaskService`.

> Ownership must come exclusively from a JWT that ASP.NET Core has successfully validated.

> The test must exercise both real HTTP application pipelines.

The actual supplied request specified M4A shared composition/startup, M4B register/login/public/me, M4C all task CRUD verbs, two-host HTTP-to-HTTP JWT proof, absolute shared storage, preserved seeds, real pipeline testing, safe errors, CORS, documentation and no M5/commit. Required task fields come from the canonical project/assessment and existing M1 contracts.

### Representative complete REST-generation prompt for presentation

This is a consolidated representative prompt describing the actual M4 work, not a claim that the following paragraph was sent verbatim earlier:

> Generate the M4 .NET 10 controller-based REST APIs for this personal task manager, reusing its existing Clean Architecture services and explicit SQLite repositories. Tasks contain title, description, status, due_date (a calendar date exposed as camelCase dueDate), and belong to the validated JWT user. TaskManager.Api exposes authenticated GET collection/by-ID, POST, PUT and DELETE. TaskManager.Auth.Api independently exposes registration/login and explicit public/current-user endpoints. Require one externally configured absolute SQLite file and shared HS256 issuer/audience/key validation; Auth.Api alone seeds after schema initialization. Use correct HTTP statuses, safe ProblemDetails and restricted Angular-origin CORS. Prove login-issued JWT acceptance across both real HTTP pipelines and cross-user CRUD isolation. Do not use Entity Framework, Dapper, MediatR, extra layers or implement Angular.

## M4 Representative Generated REST Output

Actual excerpt from `src/backend/TaskManager.Api/Tasks/TasksController.cs`:

```csharp
[HttpPost]
public async Task<ActionResult<TaskResult>> Create(TaskRequest input, CancellationToken cancellationToken)
{
    var result = await tasks.CreateAsync(CurrentIdentity.UserId(User), input.ToInput(), cancellationToken);
    return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
}
```

The controller is protected at class level. The bearer middleware validates cryptography/lifetime and exactly one positive integer subject before actions run. The request has only editable fields, with no ownership input. Existing TaskService/repository flow scopes all access to that validated owner. Results contain server-owned ID/UserId plus title/description/status/dueDate. PUT returns the updated representation with 200, DELETE 204, and missing/inaccessible resources share 404.

## M4 Observed Validation and Corrections

Initial composition/controllers and tests were authored in the same implementation cycle. This is not represented as universal test-first development, and no M1/M3 business rules were rewritten.

- An initial patch was rejected because it contained delete/add operations targeting the same Program.cs path; it was reapplied as an update. A test-support directory creation also failed and was explicitly created before retrying. These tooling issues are not red behavior evidence.
- The initial test build found seven omitted cancellation-token arguments in the new startup tests; those callers were corrected without changing existing repository interfaces.
- The first full HTTP run failed during startup because WebApplicationFactory's app-configuration callback occurred after early fail-fast composition. The fixture now supplies test settings through early host configuration, retaining app configuration for deterministic precedence. Production fail-fast validation was preserved; no environment-wide secret mutation or authentication bypass was introduced. Configuration failures from that run are not claimed as meaningful endpoint red/green evidence.
- With configuration fixed, all Auth HTTP tests and startup/token tests passed, but nine task cases failed because valid POST requests returned 500 instead of 201. Review found the generated record DTO put Required metadata on the property; MVC record binding expects validation metadata on the constructor parameter. Moving `[property: Required]` to `[Required]` made task creation work and retained omitted/null dueDate 400 handling. The affected task suite then passed all 49 cases. This is an observed correction of generated code, not reconstructed TDD history.
- Added configured-origin CORS tests, safe unexpected 500 tests in both pipelines, required-expiration and not-before rejection, and assertions that the hosts' actual content roots differ. Those later verification tests are not claimed as pre-implementation tests.
- M4 uses only the required framework packages: JwtBearer 10.0.12 for real middleware and Mvc.Testing 10.0.12 for integration tests. No forbidden direct/transitive packages were found.

Final validation: build 0 warnings/errors; 65 Application + 48 Infrastructure + 23 Auth HTTP + 55 Task/foundation/startup = 191 backend tests passed, none failed/skipped. The full command used the existing ignored HostLauncher described in the M3 runner section. Dependency inventory, Angular scaffold build/tests and diff checks are recorded with commands/results in M4_COMPLETION.md. Initial restricted build/inventory attempts could not read protected NuGet configuration; approved retries succeeded. Initial restricted Angular build exited without useful diagnostics; the approved bundled-Node retry passed. No machine configuration or application authentication was weakened.

## M4 Security and Edge Cases

Both pipelines reject malformed/unsigned/wrong-key/wrong-issuer/wrong-audience/wrong-algorithm/expired/not-yet-valid/missing-expiration tokens and missing/duplicate/nonpositive/overflow/nonnumeric subjects. Validation uses zero skew, explicit HS256 and unmapped claims; failure responses contain no token-validation internals. Authentication results expose no passwords/hashes, /me exposes no token, unknown user/wrong password share one safe response, and unexpected repository exceptions return generic 500 ProblemDetails with no SQL/stack/signing key.

Real HTTP tests cover valid and malformed binding, required calendar dates, invalid statuses/titles, unchanged tasks after failed updates, Location routing, all CRUD verbs, empty collections, missing IDs, ownership forgery via body/query, and cross-user list/read/update/delete isolation. Auth.Api tokens come from actual HTTP registration/login and are consumed by Task.Api's independent JWT middleware. Both hosts use real isolated temporary SQLite files with ephemeral test keys. No fake principals are shared.

Startup tests prove Task.Api creates schema first without demo data, Auth.Api initializes/seeds normally, different content roots share the configured file, new-file concurrent startup, and repeated/concurrent startup preserves changed demo credentials, existing username casing, edits and deleted tasks. The existing persistence factory/seeder/initializer behavior is unchanged.

The REST prompt/output and backend HTTP evidence are now present. Browser/frontend form validation and the final presentation remain future M5–M8 deliverables. Earlier readiness tables/checkpoint statements above remain historical evidence rather than being rewritten to imply M4 existed earlier.

## M5 actual request and generated work

Actual request excerpts: "Implement M5 only: Angular Authentication", "Do not implement task CRUD UI or task-management features. Those belong to M6", and "Do not invent frontend request/response contracts" (formatting omitted). The request also required sessionStorage, two API URLs, scoped interception/no leakage, authoritative /me restoration, guards/logout, accessible forms, tests and actual two-host Angular forwarding evidence.

Reviewed canonical docs, actual M4 controllers/policies and scaffold first. Registration actually returns identity plus JWT; the conditional no-token registration flow does not apply. DEC-017 records direct session establishment without hidden login or backend changes. Actual generated interceptor output:

```typescript
if (token && error instanceof HttpErrorResponse && error.status === 401)
  auth.invalidate(token);
return throwError(() => error);
```

The interceptor obtains token only for exact configured protected API origins/paths, excludes anonymous auth calls and propagates errors. AuthService compares request token with current state before invalidating, preventing old/concurrent 401 responses from deleting a newer session. Restoration uses /me for safe identity; exp decoding is UX only. Only JWT is persisted, never passwords/response objects. Forms match backend UTF-16/whitespace rules; backend remains authoritative.

Tests/implementation were co-authored; no universal test-first chronology. Observed corrections: patch parent directories were created; first router assertions navigated before logout redirect completed. Logout now returns its Router promise and tests await it. Service-destruction timer cleanup was added during review. A traceability patch had incorrect context and was corrected. Existing Prettier formatted new files. No dependencies added.

Actual validation: 36 regular Angular cases plus one opt-in live two-host case; final live run 37 passed, normal run 36 passed/1 intentional skip. Production build passed; backend build zero warnings/errors and all 191 tests passed with existing HostLauncher. Restricted NuGet/config, Angular build/cache/write and host runtime failures required approved retries; these are environment failures, not test-first behavior proof. PowerShell Stop-Process failed internally; only inspected validation processes were terminated through .NET Process API. Sandboxed registration returned safe 500; normal-runtime relaunch passed. No authentication bypass or machine-security weakening was used. Restricted GENAI file writes also required an approved documentation-only append.

Live probe obtains JWT through real Angular AuthService registration/login, persists it, then uses the real interceptor for Auth.Api /me and Task.Api GET /api/tasks. Task.Api accepted it and returned an empty collection for the new account. Only URL/header-presence booleans were observed, never token/secret logging. Probe stays test-only. Tests prove leakage rejection, invalid/expired/blocked storage, failed /me, malformed identity, expiry, logout/restoration race, old/concurrent 401s, guards and safe form/loading errors.

Browser evidence: registration, keyboard login, generic wrong-password failure, links, same-tab reload, logout and post-logout protection, labels, auth screenshots/bounds and clean current console. Requested viewport overrides did not consistently match measured dimensions; only observed narrow bounds/screenshots are claimed. Commands/evidence limits are in M5_COMPLETION.md. M6 task UI, M7 final full-system checks and M8 presentation/clean-clone review remain. Historical records above are preserved.
