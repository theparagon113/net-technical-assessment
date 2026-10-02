# M6 Completion — Angular Task CRUD

## Implemented

M6 only is complete and verified. Protected /tasks now provides the authenticated user's task list, create/edit forms, status changes and inline confirmed deletion. Loading, empty state, disabled submissions, validation, safe API errors and retry/reload are explicit. Success state uses persisted POST/PUT results; failed writes retain drafts/rows and do not claim success. Ownership IDs are neither displayed nor editable.

M5 authentication, storage, guards and interceptor remain the established session mechanism. TaskService uses only TaskManager.Api. No backend behavior, API contract, dependency or token-storage change was required. No commit, M7 hardening or M8 submission/presentation work was started.

## Files changed/created and purpose

Paths below are repository-relative. Web means src/frontend/task-manager-web.

| File | Purpose |
| --- | --- |
| Web/src/app/tasks/task.models.ts | Typed M4 request/result models; centralized numeric status labels; calendar transforms and backend-compatible UX validators. |
| Web/src/app/tasks/task.service.ts | Task.Api-only GET/POST/PUT/DELETE through centralized URL and existing interceptor; ten-second timeouts. |
| Web/src/app/tasks/tasks-page.ts | Local signal state, Reactive Forms, list/create/edit/delete/error handling, lifecycle cleanup and duplicate/overlap prevention. |
| Web/src/app/tasks/tasks-page.html | Accessible labels/help, loading/empty/error feedback, field form, list and inline delete confirmation. |
| Web/src/app/tasks/tasks-page.css | Auth-consistent desktop/mobile layout, wrapping, stacked mobile form and task cards. |
| Web/src/app/tasks/task.models.spec.ts | Numeric status, real calendar/leap/bounds/date round-trip and trimmed UTF-16 boundary coverage. |
| Web/src/app/tasks/tasks-page.spec.ts | HTTP/component list/create/edit/delete, safe failures/retries, preserved drafts, server-generated results, escaping, timeout and overlapping-operation coverage. |
| Web/src/app/tasks/tasks.live.spec.ts | Opt-in M6_LIVE real Angular/page/service/interceptor integration with both hosts and concurrent-delete 404 recovery. |
| Web/src/app/app.routes.ts | Replace guarded SessionPage placeholder with TasksPage. |
| Web/src/app/auth/session-page.ts (deleted) | Remove superseded placeholder. Existing historical M5 documentation remains unchanged. |
| Web/src/app/core/auth/auth.spec.ts | Answer new task-list requests during existing route/guard tests; retain all M5 assertions. |
| Web/README.md | Document current CRUD feature and both optional live probes. |
| README.md | Current milestone status, task usage, numeric/date contract and live M6 test command. |
| docs/PROJECT_DEFINITION.md | Update current status to M0–M6 and identify remaining M7/M8 work. |
| docs/USER_STORY.md | Record implemented task UI acceptance behavior. |
| docs/REQUIREMENTS_TRACEABILITY.md | Audit all 47 requirement IDs; update applicable frontend/field/evidence rows and append M6 audit. |
| docs/DECISIONS.md | DEC-018 explains local task state, numeric/date mappings, persisted success and failure/reload trade-offs. |
| docs/GENAI.md | Actual request excerpts, generated sample, observed corrections and verification evidence; no fabricated TDD history. |
| docs/M6_COMPLETION.md | This completion report. |
| .gitignore | Exclude disposable M6 validation logs, process metadata and screenshots under .local/m6-*. |

No package.json, package-lock.json or backend source changes. Existing M0–M5 completion reports are preserved. Local validation DB/logs/screenshots and TestResults diagnostics are not submission artifacts.

## Status mapping

The backend enum remains numeric. TaskStatus and taskStatuses centrally map 0 to Pending, 1 to In progress and 2 to Completed. The select uses ngValue so the request contains a number, not a string. Templates obtain labels from the central mapping. Tests cover all numeric values and reject a string status. Live CRUD exercises 0, 1 and 2.

## Due-date transformation

The inspected M4 contract is required .NET DateOnly, serialized as yyyy-MM-dd, rather than a timestamp. HTML date inputs use that same calendar representation. Named apiDateToInput/inputDateToApi transforms validate real Gregorian dates in years 0001–9999 and preserve the exact string. No Date constructor, UTC conversion, DatePipe or local timezone calculation occurs. The visible date uses ISO calendar text. Required date, leap-day, century rules, impossible dates, timestamps and min/max bounds are covered; past dates remain allowed. Actual 2024-02-29 and 2026-10-03 values survived create/edit/reload/browser refresh without shifting.

## API and authentication integration

TaskService uses API_CONFIG.taskApiBaseUrl plus /api/tasks for list, create, update and delete. Components contain no API URL or token-header logic. Requests contain only title, description, numeric status and dueDate; response id/userId match TaskResult but are not ownership inputs. The original authInterceptor supplies JWT and handles protected 401s; guard/sessionStorage/expiry/logout remain unchanged. Auth.Api handles registration/login/current-user restoration only.

## Tests added or updated

Four mapping tests plus ten page/HTTP cases were added. One opt-in live M6 test was added. Existing auth/router tests now consume the task list request emitted by the new screen; all prior auth assertions remain.

Page coverage includes initial loading and empty state, list retry and safe error text, validation/no invalid request, duplicate-submit suppression, persisted create result and form clearing, edit prepopulation/numeric update/date, failed PUT retry, intentional deletion/cancellation/failed DELETE retry, failed POST preservation, escaped description rendering, overlap prevention and unconfirmed timeout recovery. Existing M5 tests continue to prove guard/401/session behaviors.

Implementation/tests were co-authored. The observed failures and corrections are recorded in GENAI.md; no universal test-first history is claimed.

## Validation performed

Used bundled Node 24.19.0 and existing Angular CLI 22.2.1. Direct node invocation of node_modules/@angular/cli/bin/ng.js test/build performs the same CLI operation as the package npm test/build scripts, avoiding system Node version ambiguity.

| Command | Final result |
| --- | --- |
| npm test -- --watch=false (equivalent bundled Node CLI invocation) | 50 passed, 2 intentional opt-in skips; 5 files passed/2 skipped; exit 0. |
| M5_LIVE=1 and M6_LIVE=1, same test command | 52 passed, 0 failed/skipped; 7 files passed; exit 0. |
| npm run build (equivalent bundled Node CLI invocation) | Production build passed; 314.57 kB raw / 83.03 kB estimated transfer; no budget warnings; exit 0. |
| dotnet build TaskManager.sln --no-restore --verbosity minimal | 9 projects; 0 warnings/errors; exit 0. |
| dotnet test TaskManager.sln --no-restore --verbosity minimal --diag TestResults/m6-final.log -- RunConfiguration.DotNetHostPath=C:/Maethrillian/NET-TechnicalAssessment/TestResults/HostLauncher/bin/Debug/net10.0/HostLauncher.exe | Application 65 + Infrastructure 48 + Auth.Api 23 + Task.Api 55 = 191 passed, none failed/skipped; exit 0. |
| dotnet list TaskManager.sln package --include-transitive --no-restore | Passed; no Entity Framework, Dapper or MediatR direct/transitive dependency. |
| Existing Prettier on affected Angular source | Passed. |
| git diff --check | Passed after EOF whitespace cleanup; only line-ending notices. |

Restricted production build initially exited without diagnostics; approved normal-runtime retry passed. Package inventory initially could not read protected NuGet.Config; approved retry passed. Existing machine-specific HostLauncher was reused unchanged. These are environment limitations, not behavior-test failures or application security exceptions.

## Real integration verification

Both real API hosts ran on 5150/5149; Angular ran on 4200. Hosts shared disposable absolute .local/m6-validation.db and the same temporary random signing configuration, with localhost:4200 CORS. No signing key or JWT was printed/committed.

M6 live probe uses real Angular AuthService, TasksPage, TaskService, HttpClient, guard and interceptor, with no HttpTestingController or manually inserted JWT header. It confirms anonymous blocking, registration/login, empty list, create, both status updates, description/title/date persistence, reload and delete. A simulated another-tab deletion then causes real PUT 404; UI preserves the draft/list, shows recovery feedback and no success, and reload confirms the empty server state. M5 live probe continues to prove /me restoration from stored JWT and logout.

Browser checks separately confirmed:
1. Anonymous /tasks redirects to /login.
2. Registration reaches protected task empty state; later login succeeds via Enter.
3. Create shows persisted title/description, In progress and 2024-02-29.
4. Edit prepopulates every field; update shows Completed and 2026-10-03.
5. Full refresh retains the session and persisted task/date.
6. Delete confirmation is inline; Cancel preserves the task; Confirm delete returns to empty state.
7. Logout returns to login and later direct /tasks navigation is blocked.
8. Measured desktop viewport 1280x900, document width 1280 and panel width 1040.
9. Measured mobile viewport 390x844, list document width 390/panel 358; edit form document width 375 with scrollbar and all four controls within page bounds. No horizontal overflow.
10. Browser console read returned no warning/error entries.

Screenshots were inspected and saved at .local/m6-tasks-desktop.png and .local/m6-tasks-mobile.png. Temporary viewport override was reset. Only validation processes launched for M6 were stopped afterward. No full device/browser matrix is claimed; M7 owns final full-system review.

## Assumptions

Canonical repository assessment transcription is authoritative for agent work. Existing local ports, central API URLs, configured CORS and accepted M5 sessionStorage trade-off remain valid. Due dates are calendar deadlines with ISO display, not timed instants. Backend remains authoritative for validation/ownership. An unconfirmed network write may have persisted; reload is the recovery path before retrying.

## Deviations and backend changes

No assessment, project-definition, user-story, milestone or architecture deviations. No backend changes or new dependencies were required. The suggested project-definition component split is advisory; one focused TasksPage is sufficient for this feature, with service/models separate.

## M6-specific human review items

Review DEC-018 and the simple inline create/edit/delete flow, ISO calendar-date display, and 404/timeout Cancel/Reload recovery wording. Live tests require a disposable database and intentionally create test accounts. No unresolved M6 behavior or failing required validation remains. M7 full-system hardening and M8 clean-clone/presentation work are separate and have not been started.

Stop after M6. No Git commit was created.
