# M7 Full-System Hardening

Publication note: this is a historical milestone record. Machine-specific executable paths in recorded commands are generalized as labeled placeholders; original scope, failures, successful retries, counts and evidence limits are preserved. Ignored launcher/runtime helpers are historical provenance, not reviewer prerequisites.

## Status

M7 only is complete. Automated, real HTTP and browser checks passed, including final browser Confirm delete after the developer's explicit action-time approval. Automatic approval review initially rejected that action; it was not bypassed. No commit was created and M8 was not started.

Work began from a clean working tree. Historical M3–M6 and reconciliation reports are preserved. This review uses the canonical assessment transcription; no independent inspection of the proprietary PDF is claimed.

## Areas reviewed and findings

| Area | Review / result |
| --- | --- |
| Domain / Application | Title/description trimming and UTF-16 bounds, username identity/control rules, preserved password spaces/bounds, enum validation, calendar dates and ownership. No defect found; no business rules changed. |
| Infrastructure | All influenced SQL values parameterized; owner predicates on reads/writes; explicit mapping and disposal; foreign keys; deterministic duplicate/race handling; transactional initialization/seeding. Real SQLite tests pass. No defect found. |
| Both API hosts | Controllers, JWT pipeline, safe ProblemDetails, HTTP status codes, malformed binding, registration/login/public/me and task CRUD. Existing invalid-token and internal-failure coverage passes. No production backend defect found. |
| Startup/configuration | Shared external absolute database, shared JWT validation, Task.Api-first independence/no seed, Auth.Api-only seed and preservation, concurrent/repeated startup, exact configured CORS origin. Existing startup/HTTP tests pass. |
| Angular session/integration | Separate URLs, scoped interceptor, guards, session restoration, expiration/401 invalidation, logout and JWT-only sessionStorage. Regular and real-host tests pass; no session change required. |
| Angular CRUD/recovery | List/empty state, create/edit/status/date persistence, confirmation/cancel, failed writes retaining drafts, loading/duplicate submission and retry/reload. Real concurrent-delete PUT 404 recovered in the browser. |
| Responsive UI | Found actual overflow with a valid 64-character unbroken username at 390px. Fixed task host/grid minimum sizing and username wrapping. |
| Repository artifacts | Six generated M5 runtime files were tracked despite earlier documentation describing validation artifacts as local. Untracked these files while retaining local copies; ignore all .local artifacts, including README default runtime logs. No key/token/password hash was found in those tracked logs. Git history was not rewritten. |
| Dependencies / organization | All nine project dependencies reviewed; no EF, Dapper, MediatR or replacement ORM/mediator. Cohesive modules and dependency directions preserved. No dependency added. |

## Exact fixes and files changed

- `src/frontend/task-manager-web/src/app/tasks/tasks-page.css`: add min-width:0 to the feature host and toolbar text container, and overflow-wrap:anywhere to signed-in text. Before: 390px viewport, 541px document width and 524.43px panel. After: 390px document width and 358px panel. No visual redesign.
- `.gitignore`: ignore the complete /.local/ runtime directory instead of just M6-prefixed artifacts.
- Remove from Git tracking only: `.local/m5-auth-error.log`, `m5-auth-output.log`, `m5-auth-ui.jpg`, `m5-processes.json`, `m5-task-error.log`, `m5-task-output.log`. Local copies remain. Their staged removals are part of the reviewable diff, not a commit.
- `tests/TaskManager.Api.Tests/TaskHttpTests.cs`: extend the existing invalid-input theory with impossible leap day, timestamp date and string enum cases. Each exercises POST and PUT, checks 400 ProblemDetails and verifies persisted task/list preservation.
- `docs/GENAI.md`: append observed M7 corrections, validation and evidence limits.
- `docs/REQUIREMENTS_TRACEABILITY.md`: record M7 evidence and completion without changing the 47 requirement IDs or claiming M8 deliverables.
- `README.md`, `docs/PROJECT_DEFINITION.md`, `docs/USER_STORY.md`: correct current milestone status to M7 verified, retaining historical reports and M8 scope.
- `docs/M7_COMPLETION.md`: this report.

No other production code, API contract, domain policy, session strategy or dependency changed. Ignored TestResults/m7-* helpers, diagnostics, screenshots and disposable runtime files are validation artifacts.

## Tests and methodology

Three additional Task.Api theory cases cover meaningful serialization boundary gaps: yyyy-MM-dd shape is insufficient for an impossible Gregorian date; a timestamp must not become a calendar date; enum names must not replace the established numeric contract. These were added after real HTTP verification and passed immediately; no failing-test-first chronology is claimed. Existing Application, real SQLite, both HTTP suites and Angular cases are preserved.

The CSS regression was reproduced and verified through actual browser layout measurements/screenshots. No jsdom layout test or CSS implementation-mirroring assertion was added.

## Exact validation commands and results

Run from the repository root unless specified. Bundled Node executable:

`<supported-node>`

| Command | Result |
| --- | --- |
| `dotnet build TaskManager.sln --no-restore --verbosity minimal` | Baseline and final pass; nine projects, 0 warnings, 0 errors. |
| `dotnet test TaskManager.sln --no-restore --no-build --verbosity minimal --diag TestResults/m7-baseline.log -- RunConfiguration.DotNetHostPath=<historical-launcher-path>` | 191 passed: Application 65, Infrastructure 48, Auth.Api 23, Task.Api 55; no failures/skips. |
| Same command with `--diag TestResults/m7-final.log` | 194 passed: 65 + 48 + 23 + 58; no failures/skips. |
| `dotnet list TaskManager.sln package --include-transitive --no-restore` | Passed after normal-runtime retry for denied user NuGet.Config access; no forbidden packages. |
| `& '<bundled Node>' node_modules/@angular/cli/bin/ng.js test --watch=false` (frontend directory) | Baseline 50 passed, 2 intentional live skips; no errors/warnings. |
| `$env:M5_LIVE='1'; $env:M6_LIVE='1'; & '<bundled Node>' node_modules/@angular/cli/bin/ng.js test --watch=false` (frontend directory, both hosts running) | After CSS fix: 52 passed, no failures/skips, seven files. |
| `& '<bundled Node>' node_modules/@angular/cli/bin/ng.js build` (frontend directory) | Restricted run exited 1 with no diagnostics; normal-runtime retry passed baseline, 314.57 kB raw / 83.03 kB estimated transfer. |
| `$env:PATH='<supported-node-bin>;' + $env:PATH; npm test -- --watch=false` (frontend directory) | Final 50 passed, 2 intentional live skips, five files passed/two skipped; exit 0. |
| Same supported PATH setup, `npm run build` (frontend directory) | Final production build exit 0, 314.67 kB raw / 82.98 kB transfer, no budget/compiler warnings. |
| `& ./TestResults/m7-start.ps1` | Launched real task/auth hosts and Angular with disposable absolute TestResults/m7-runtime/m7.db, ephemeral random shared key and localhost:4200 CORS; no key printed. |
| `& '<bundled Node>' TestResults/m7-http.mjs` | 92 real HTTP/security assertions passed; no JWT printed. |
| `& '<bundled Node>' TestResults/m7-concurrent-delete.mjs` | Independent authenticated HTTP session deleted the generated browser task to induce a real recoverable 404. |
| `git rm --cached -- .local/m5-auth-error.log .local/m5-auth-output.log .local/m5-auth-ui.jpg .local/m5-processes.json .local/m5-task-error.log .local/m5-task-output.log` | Six generated files removed from tracking; local copies retained. |
| `git diff --check`; `git diff --cached --check`; `git status --short` | Final checks pass; expected code/documentation changes and six staged artifact removals only. CRLF conversion notices are the sole Git warnings. |
| `git ls-files .local '*.db*' '*.log' '*secret*'`; `git check-ignore .local/auth-output.log .local/m5-auth-output.log TestResults/m7-runtime/processes.json` | No remaining tracked runtime/database/secret files matched; local outputs are ignored. |

Direct ng invocations above use the same existing package scripts/CLI, without adding tools or dependencies. The existing ignored HostLauncher was reused unchanged for this machine's documented VSTest loopback redirection; ordinary local dotnet test portability is not newly claimed. Production build, dependency inventory and background runtime launch required approved normal-runtime execution. These tooling limitations did not weaken application authentication or count as failing/passing behavior assertions.

## Real HTTP and security evidence

Auth.Api at localhost:5150, Task.Api at localhost:5149 and Angular at localhost:4200 used one disposable database. The HTTP probe registered two independent users, logged in, called /me, sent the actual Auth.Api-issued JWT to Task.Api, and exercised create/read/list/edit/status/date/delete/deletion verification. It verified owner spoofing via request userId fails, other-user lists are empty, and GET/PUT/DELETE return identical 404 titles for inaccessible and missing tasks.

Both live hosts rejected missing, malformed and tampered tokens. The full HTTP-pipeline suites additionally reject expired, future, unsigned, wrong-key/issuer/audience/algorithm, missing-expiration and invalid/duplicate identity tokens. These rejection checks use actual middleware over HTTP, not direct service validation. Safe injected unexpected 500 responses are covered through both existing HTTP pipelines; no production failure route was added.

Real HTTP adversarial inputs covered malformed JSON, missing values, username controls/bounds, short password, whitespace/long title, long description, invalid numeric/string status, null/impossible/timestamp dates, duplicate casing and wrong password. SQL syntax round-tripped as text and did not affect schema; script-like descriptions rendered as literal text. Status 0/1/2, leap day 2024-02-29 and lower calendar bound 0001-01-01 survived persistence without time-zone conversion. Both real hosts' OPTIONS preflights allowed only the configured origin and rejected an untrusted origin.

Startup independence, shared database despite different roots, concurrent/idempotent initialization and seed/credential/edit/deletion preservation were revalidated by existing StartupTests, rather than redundantly rewriting that coverage.

## Real Angular/browser evidence

Codex in-app browser checks:

1. Anonymous /tasks redirects to /login.
2. Register a new maximum-length username; token-bearing registration reaches protected empty list.
3. Create task with literal script-like description, In progress and 2024-02-29.
4. Edit prepopulates fields; save changes title/status to Completed and date to 2026-10-03.
5. Full refresh retains session and persisted task/date through /me restoration.
6. Delete confirmation appears; Cancel preserves task.
7. Independent real HTTP deletion while the browser edits causes PUT 404. Draft/row remain and safe recovery feedback appears; Cancel/Reload restores the empty list.
8. Logout returns to login; subsequent direct /tasks is blocked; login via Enter succeeds again.
9. Desktop measured 1280x900, document 1280px, panel 1040px. Mobile measured 390x844, empty-list document 390px, panel 358px; form document 375px with vertical scrollbar, all controls bounded approximately 55.73–318.94px. No horizontal overflow after fix.
10. After explicit developer approval, create a separate disposable task, Confirm delete, observe Task deleted and the empty list, then sign out.
11. Console warning/error query returned [] after normal CRUD/refresh, intentional 404 recovery and final confirmed deletion.

Both opt-in Angular live tests passed using real HttpClient, AuthService, TasksPage, TaskService, guards and interceptor against both hosts; they cover complete CRUD including confirmed deletion, restoration, logout and 404 recovery. Browser final Confirm delete passed after explicit developer approval. Temporary viewport override was reset. Saved evidence: ignored TestResults/m7-runtime/mobile.jpg (form) and mobile-final.jpg (confirmed deletion/empty list). Browser setup initially failed before the local server was started; creating a tab after startup worked. Only the inspected API/Angular process trees launched by TestResults/m7-start.ps1 were stopped afterward using taskkill /PID <recorded-id> /T /F; other processes were untouched.

## Warnings reviewed

- Backend and Angular builds: no compiler/nullable/budget warnings. All final tests pass with no runtime exception or flaky result observed.
- Real HTTP-only development profiles emit one `Failed to determine the https port for redirect` warning per API. Those profiles deliberately have no HTTPS listener. HTTPS profiles remain supported; the HTTPS policy was retained, without log suppression or weakening. No functional defect was found in the documented HTTP development topology.
- Browser console: no warning/error entries in inspected flows. Deliberate HTTP rejection cases behaved as expected.
- Git line-ending conversion notices are repository/environment behavior, not whitespace failures.

## Assumptions, deviations and review items

Assumptions: canonical assessment transcription remains authoritative for agent work; existing localhost ports and accepted short-lived sessionStorage strategy remain valid; generated users/tasks are disposable assessment verification data; deliberate HTTP development remains appropriate. No new business, architectural or product assumptions were introduced.

No implementation deviation from M7, assessment, project definition, user story or existing decisions. No new architectural decision warrants DECISIONS.md changes. The browser deletion approval gate was resolved by the developer and the actual action was verified. M8 clean-clone/submission/presentation work remains untouched.

Human review: inspect the small responsive fix, three serialization regression cases and staged removal of generated files. Existing Git history still contains the old nonsensitive runtime artifacts; no history rewrite was requested. The local runner workaround and intentional HTTP-profile HTTPS warning remain documented environment/configuration limits. No unresolved M7 product defect, failed required check or approval gate remains.
