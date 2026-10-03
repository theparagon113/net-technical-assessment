# Generative AI Usage

## Tools Used

Codex assisted scoped planning, implementation, explicit SQL, tests, Angular UI and documentation. This record summarizes observed M2–M8 work and the human requirements reconciliation. It does not reconstruct unrecorded earlier prompts or claim that every generated suggestion was accepted.

## REST API Prompt

The recorded M4 request includes these actual technical excerpts:

> Implement **M4 only: Two-host controller-based Web APIs**.
>
> Use the existing `TaskService`.
>
> Ownership must come exclusively from a JWT that ASP.NET Core has successfully validated.
>
> The test must exercise both real HTTP application pipelines.

The complete request also specified shared composition, registration/login/public/me, all task CRUD verbs, cross-host token acceptance, absolute shared storage, preserved seeds, safe errors, CORS and real HTTP tests.

**Representative consolidated prompt:** The following describes the actual M4 work for presentation; it is not a verbatim historical prompt.

> Generate the M4 .NET 10 controller-based REST APIs for this personal task manager, reusing its existing Clean Architecture services and explicit SQLite repositories. Tasks contain title, description, status, due_date (a calendar date exposed as camelCase dueDate), and belong to the validated JWT user. TaskManager.Api exposes authenticated GET collection/by-ID, POST, PUT and DELETE. TaskManager.Auth.Api independently exposes registration/login and explicit public/current-user endpoints. Require one externally configured absolute SQLite file and shared HS256 issuer/audience/key validation; Auth.Api alone seeds after schema initialization. Use correct HTTP statuses, safe ProblemDetails and restricted Angular-origin CORS. Prove login-issued JWT acceptance across both real HTTP pipelines and cross-user CRUD isolation. Do not use Entity Framework, Dapper, MediatR, extra layers or implement Angular.

The prompt constrains scope, reuses existing contracts, names the assessment fields and ownership boundary, and requests observable security/HTTP evidence.

## Representative Generated Output

Actual generated excerpt from [TasksController.cs](../src/backend/TaskManager.Api/Tasks/TasksController.cs):

```csharp
[HttpPost]
public async Task<ActionResult<TaskResult>> Create(TaskRequest input, CancellationToken cancellationToken)
{
    var result = await tasks.CreateAsync(CurrentIdentity.UserId(User), input.ToInput(), cancellationToken);
    return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
}
```

The class has `[Authorize]`; middleware validates the token before `CurrentIdentity` extracts its owner. `TaskRequest` has only editable fields. TaskService enforces business rules and calls owner-aware persistence. POST returns 201 with GET-by-ID Location; PUT returns 200 with saved data, DELETE 204, and missing/inaccessible records share 404.

Actual generated persistence excerpt from [SqliteTaskRepository.cs](../src/backend/TaskManager.Infrastructure/Persistence/Repositories/SqliteTaskRepository.cs):

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

Parameters preserve SQL-like input as data. The combined task/owner predicate and affected-row result enforce isolation and detect a row disappearing before mutation.

Actual generated calendar mapping from [task.models.ts](../src/frontend/task-manager-web/src/app/tasks/task.models.ts):

```typescript
// DateOnly and HTML date inputs share a calendar string. Never convert through Date/UTC.
export function inputDateToApi(value: string): string {
  return apiDateToInput(value);
}
```

The shared validator checks real Gregorian dates and .NET DateOnly bounds. Passing the calendar string unchanged prevents timezone shifts.

## Validation

Suggestions were checked against the canonical assessment, application contracts, actual source and dependency boundaries. Verification included solution builds, Application unit tests, real SQLite tests, both WebApplicationFactory HTTP pipelines, Angular regular/live tests, production builds, browser layout/console checks and manual security review.

| Recorded milestone | Backend passes | Angular passes | Evidence |
| --- | --- | --- | --- |
| M2 | 71: 45 Application + 26 Infrastructure | 2 scaffold | Observed persistence red/green below; no HTTP behavior yet. |
| M3 | 113: 65 Application + 48 Infrastructure | 2 scaffold | [M3 report](M3_COMPLETION.md); HTTP composition still deferred. |
| M4 | 191: 65 + 48 + 23 Auth + 55 Task/foundation/startup | 2 scaffold | [M4 report](M4_COMPLETION.md); both real HTTP pipelines. |
| M5 | 191 | 36 regular; 37 with live auth | [M5 report](M5_COMPLETION.md); auth/session and browser checks. |
| M6 | 191 | 50 regular; 52 with both live probes | [M6 report](M6_COMPLETION.md); task CRUD and browser checks. |
| M7 | 194: 65 + 48 + 23 + 58 | 50 regular; 52 live | [M7 report](M7_COMPLETION.md); 92 real HTTP/security assertions and responsive regression. |
| M8 | 194 | 50 regular; 52 live | [M8 report](M8_COMPLETION.md); local fresh clone, production build, 92 HTTP assertions and browser smoke. |

These are recorded development results, not tests rerun for this publication refinement. Regular M5 tests skipped one opt-in case; regular M6–M8 skipped two. Enabled live suites had no skips. Final backend suites had no failures/skips. M8 production build passed with no warnings, 314.67 kB initial raw / 82.98 kB estimated transfer.

HTTP tests exercise controllers, routing/model binding, JWT middleware, services and real SQLite through TestServer. Live probes use Angular HttpClient/interceptor against both running hosts. Browser checks verify rendering, keyboard/forms, CRUD/session/recovery, responsive bounds and the console. M8 measured actual narrow/wide dimensions instead of claiming requested overrides succeeded; its browser deletion check used Cancel, while live probes covered deletion. M7 retains actual browser Confirm-delete evidence.

### Observed TDD and execution limits

- **M2:** Repository tests preceded SQL implementation. Initial restricted/timeout runs did not reach assertions. A later run reached 22 failing repository/seeder tests after schema initialization; user SQL yielded 2 passes/20 failures, then task SQL/seeding yielded 22 passes. Four later rollback/hash-input cases were post-implementation verification. All 26 Infrastructure and 45 Application cases finally passed.
- **M3:** AuthService and initial tests were co-authored. Four identity tests preceded persistence changes, but runner failures prevented verified red evidence. The diagnostic-redaction regression below has an observed failing/passing cycle.
- **M4–M6:** Initial implementation and tests were co-authored. Actual failures/corrections are recorded below; no universal test-first claim is made.
- **M7:** Three serialization-boundary cases were added after HTTP verification and passed immediately. The layout defect was reproduced and corrected in a browser, without treating jsdom as layout proof.

Earlier restricted NuGet/build/runtime attempts and VSTest startup timeouts were environment failures, not behavior-test evidence. M3–M7 used an ignored local launcher that corrected only the testhost endpoint argument; normal VSTest/xUnit assertions remained intact. M8 passed ordinary `dotnet test` in a fresh local clone without that launcher. Its first npm wrapper selected unsupported Node and warned; an explicit supported-runtime retry passed. Historical reports preserve failures, successful retries and evidence limits.

## Corrections and Human Review

### Second API omitted from the initial plan

Human review against the original assessment identified that the initial AI-assisted plan omitted the explicit second-API requirement. The architecture was corrected after M3, before HTTP endpoint implementation proceeded.

The reconciliation added TaskManager.Auth.Api and its test scaffold, canonical requirements and traceability, then assigned two-host MVC controllers, explicit anonymous public/protected current-user endpoints, one absolute database, Auth.Api seed ownership, compatible JWT validation and separate Angular URLs. [DEC-015/016](DECISIONS.md) record the decision; the [checkpoint report](RECONCILIATION_COMPLETION.md) preserves what remained unimplemented at that time. M4 HTTP/startup tests later proved the resulting behavior, including login-issued JWT acceptance across hosts. Earlier milestones are not rewritten as though this requirement had been captured initially.

### Password-bearing diagnostic output (M3)

Security review found that the generated positional AuthInput record's default `ToString` exposed its password. A new regression test failed because a synthetic test password appeared in diagnostics. The override redacted that value; the targeted test then passed. AccessToken/AuthResult diagnostics were also redacted and covered. This was an actual generated-code correction with observed red/green evidence; it did not involve a leaked production credential. See [AuthServiceTests](../tests/TaskManager.Application.Tests/AuthServiceTests.cs).

### MVC record validation metadata (M4)

Nine task HTTP cases failed because valid POSTs returned 500 instead of 201. Review found `Required` metadata on a record property, while MVC record binding required it on the constructor parameter. Changing `[property: Required]` to `[Required]` corrected creation and retained omitted/null due-date 400 behavior. The affected suite then passed all 49 cases; later M4 coverage reached 55 task/foundation/startup cases. See [TaskRequest](../src/backend/TaskManager.Api/Tasks/TaskRequest.cs) and [HTTP tests](../tests/TaskManager.Api.Tests/TaskHttpTests.cs).

### Test configuration and frontend corrections (M4–M6)

The first M4 HTTP run failed during startup because fixture configuration arrived after fail-fast composition. Early host configuration fixed the fixture while preserving production validation; startup failures were not counted as endpoint red/green proof.

Earlier review added SQLite/sidecar ignore rules in M2 because persisted data can contain hashes. M3 replaced a brittle exact JWT claim-count assertion with absence-of-password-claims coverage while retaining signature/identity/issuer/audience/expiry checks. Its duplicate test was updated to the new application exception while direct SQL tests still verified unique error 2067; a real insert-race case verified no token issuance on conflict.

M5 router assertions initially ran before logout navigation completed; returning/awaiting its Router promise corrected that race. Service timer cleanup was added during review. In M6, existing route fixtures needed to answer the new task-list request, and the live test incorrectly expected the original due date after an earlier edit. Fixtures/assertions were corrected without dropping authentication or persistence checks. Review also found reload/mutation overlap; blocking writes while loading gained a regression test. Failed/timeout writes retain drafts and avoid false success.

### Mobile overflow and tracked runtime artifacts (M7)

A valid unbroken 64-character username produced a 541px document in a 390px viewport. Minimal sizing/wrapping changes reduced the document to 390px and panel to 358px; actual browser measurements verified the correction.

Six generated M5 runtime artifacts remained tracked despite earlier reports describing them as local. M7 removed current tracking, retained local copies and ignored all `.local` output. No key/token/hash was found in the inspected logs. Existing history was preserved and still requires publication review; the earlier report discrepancy remains documented.

### Submission documentation/tooling (M8)

Current documents still described completed UI/presentation preparation as pending, the frontend README suggested an unconfigured `ng e2e` runner, and VS Code retained a legacy Karma port-9876 launch. M8 corrected those statements/tooling and validated a local fresh clone. These were real documentation/tooling findings, not invented product defects or TDD evidence. This final pass condenses public documentation and generalizes personal paths without changing those historical facts.

## Edge Cases

- **Persistence:** SQL-like text, generated IDs, nullable descriptions, foreign keys, cancellation, leap dates under a different culture, duplicate/case/Unicode identities and insert races, collision rollback, transactional seeds and preserved credentials/edits/deletions.
- **HTTP/security:** Missing/inaccessible tasks, cross-user list/read/update/delete isolation, forged owner fields, malformed JSON/IDs, absent/null/impossible/timestamp dates, invalid numeric/string status, invalid tokens/subjects, safe duplicate/credential/500 responses and restricted CORS.
- **Frontend:** Invalid/blocked storage, expired/malformed tokens, failed `/me`, logout/restoration and old-token 401 races, exact interceptor origin/path boundaries, duplicate/overlapping operations, failed writes/timeouts, concurrent-delete 404 recovery and long usernames.

These cases have source/test or browser evidence in the linked reports; no retrospective failing-test chronology is inferred.

## Authentication and Security

Application AuthService validates credentials and uses infrastructure ports for the supported framework hasher, persisted users and JWT issuance. Passwords are hashed with random salts, omitted from responses and redacted in diagnostic contracts. Unknown-user/wrong-password failures share a generic response; equal execution timing is not claimed.

Both hosts independently validate externally configured issuer/audience/key, HS256 signature, required expiration and lifetime with zero skew/unmapped claims. Exactly one positive integer subject is required. HTTP tests obtain actual Auth.Api registration/login tokens and consume them in Task.Api; owner-aware SQL enforces isolation. Signing secrets are not committed and test keys are ephemeral.

Angular persists only the short-lived JWT, restores claims identity through protected `/me`, and attaches Bearer only to exact configured protected API boundaries. Client decoding/guards improve UX; server authentication/ownership remain authoritative. No refresh tokens, revocation or database account lookup on `/me` is implemented. sessionStorage remains accessible under XSS; logout does not revoke a captured JWT.

## Validation Handling

Domain TaskItem invariants and Application policies enforce business rules independently of HTTP/SQLite. Titles/descriptions are normalized and bounded, username identity is consistent, passwords preserve spaces within bounds, and only defined statuses are accepted. Past calendar dates are allowed.

MVC model binding catches malformed/missing HTTP input, including required nullable due-date metadata. Shared translation returns safe ProblemDetails/statuses: 400 validation, 401 authentication/credentials, 404 missing/inaccessible, 409 duplicate, generic 500 unexpected errors. Invalid updates preserve stored data.

Angular Reactive Forms mirror these boundaries for feedback and preserve unsuccessful drafts/rows. Numeric status and validated calendar strings match the actual API; no frontend ownership input or timezone conversion is introduced.

## Lessons / Human Decisions

Human assessment review established the two-host requirement and corrected generated planning. Focused services, specific repositories, explicit SQL and framework cryptography kept the implementation explainable. Live HTTP/browser checks exposed issues beyond unit-test boundaries.

Requirement traceability and manual review complement automated checks. Generated code, prompts and claims are accepted only with recorded evidence. Selected TDD cycles remain useful evidence; partial adoption stays explicit. The story, architecture, decisions and GenAI evidence support technical review; rehearsal, public publication and delivery remain human actions.
