# M5 Completion — Angular Authentication

Publication note: this is a historical milestone record. Machine-specific executable paths in recorded commands are generalized as labeled placeholders; original scope, failures, successful retries, counts and evidence limits are preserved. Ignored launcher/runtime helpers are historical provenance, not reviewer prerequisites.

## Implemented

M5 only is complete: responsive login/registration, focused signal-based AuthService, sessionStorage JWT handling, authoritative current-user restoration, scoped functional interceptor, guards and logout. Existing uncommitted M0–M4 work was preserved. No backend behavior/dependency files or historical milestone reports were changed by M5. No commit, task CRUD UI or later milestone was started.

## Files changed/created

- Web `src/app/core/api-config.ts`; `core/auth/auth.models.ts`, `auth.service.ts`, `auth.interceptor.ts`, `auth.guard.ts`, `auth.spec.ts`, `auth.live.spec.ts`.
- Web `src/app/auth/auth-page.ts`, `.html`, `.spec.ts`, `session-page.ts`; root app config/routes/template/spec and global styles.
- Root/frontend README; PROJECT_DEFINITION, USER_STORY, DECISIONS (DEC-017), GENAI, REQUIREMENTS_TRACEABILITY; personal rehearsal material subsequently removed; this report.
- No package/lockfile changes or new dependencies. Ignored `.local` contains disposable validation database/logs/process metadata/UI screenshot; TestResults contains diagnostics only.

## Routes

`/login` and `/register` are functional guest routes; established sessions redirect to `/tasks`. `/tasks` is a guarded safe-username/session placeholder with sign out. Empty/unknown paths route through the protected destination. It contains no task listing/forms/CRUD.

## Auth/session design

Actual M4 input is `{ username, password }`; both registration 201 and login 200 return `{ userId, username, accessToken, expiresAt }`. Both establish safe identity from the successful response, avoiding an extra `/me` or hidden login. Only the JWT is stored under centralized `task-manager.access-token`; no password, full response, localStorage, cookies or signing secret is persisted by application code.

Startup checks JWT shape/expiration for UX, then `/api/auth/me` authoritatively confirms safe identity before guards proceed. Failures clear state; requests time out at ten seconds. Expiry timers and on-demand expiry checks remove stale sessions. Client decoding does not verify signatures or authorize server access. Logout during restoration cannot resurrect state. Destroying the service cleans its timer.

sessionStorage is accessible to JavaScript under XSS, but limits persistence compared with localStorage. A production application could use secure HttpOnly cookies/BFF. No refresh token system was added; backend authentication/ownership remain the security boundary.

## Two-API configuration and interceptor

`core/api-config.ts` centralizes `authApiBaseUrl=http://localhost:5150` and `taskApiBaseUrl=http://localhost:5149`, matching README/M4 profiles. Injectable configuration permits test overrides; configure host base URLs without trailing slashes. It contains public URLs only. Both backend processes require identical external database/JWT settings and configured frontend CORS origin.

Bearer attaches only to exact configured origins and Auth.Api `/api/auth/me` or Task.Api `/api/tasks` and slash-delimited descendants. Query strings are allowed. Login/register/public/assets/unrelated origins/lookalike origins/path prefixes receive no token. A protected 401 invalidates only its matching token and navigates to login once; stale concurrent responses cannot remove a newer token. Feature errors still propagate. Anonymous login 401 produces generic invalid credentials without a global redirect.

## Guard/logout and forms

Guards await the single restoration promise, then require established safe user plus usable token. They do not query `/me` on every navigation. Logout immediately clears storage/state and returns the navigation promise; later `/tasks` navigation redirects to `/login`.

Reactive Forms match current backend UTF-16 lengths and whitespace/control rules: trimmed username 3–64, password 8–128 with preserved spaces, neither blank. Controls have labels, autocomplete/help text/invalid state, visible focus, semantic Enter submission, loading/disabled state and safe textual errors. Known 400/401/409/network/server failures are translated; raw ProblemDetails/internals are not rendered.

## Tests added/updated

36 regular Angular cases total (including two root cases); one optional real-host integration case. Service/interceptor/router cases cover login/storage/safe user, token-bearing registration, `/me` restoration/failures, invalid/expired storage, blocked storage, malformed identity, expiration timer, logout during restoration, exact token attachment/leakage boundaries, anonymous login 401, simultaneous protected 401s, old-token 401 and guard/guest redirects. Login/registration component cases cover invalid submission, backend-compatible boundaries, semantic submission/loading/duplicate suppression, successful navigation/password clearing and safe error/retry feedback. Tests/implementation were co-authored; no universal TDD chronology is claimed.

## Live integration validation and evidence

Both real hosts ran on 5150/5149 with one disposable absolute `.local/m5-validation.db`, the same temporary random signing key/issuer/audience, 15-minute expiry and localhost:4200 CORS. Angular ran on localhost:4200. Keys and real tokens were not printed. The opt-in live test used actual Angular HttpClient, AuthService, storage and authInterceptor without HttpTestingController or manually inserted Authorization. Its downstream observation interceptor recorded only URL/header-presence booleans. Registration/login/me succeeded; Task.Api returned authenticated 200/empty collection for the new user. A fresh service restored from the stored JWT; logout blocked the route. The probe lives only in test code.

Browser automation in Codex's in-app browser separately observed successful registration, successful login via Enter, generic wrong-password feedback, same-tab full reload staying on `/tasks`, logout reaching login, direct post-logout `/tasks` redirect and links between auth forms. Successful current-page console inspection returned no warning/error entries. Earlier unavailable-host handling was observed as safe UI feedback; early server/cache failures are environment evidence, not product success.

Responsive screenshots reviewed auth/session layout. Requested viewport overrides did not consistently match measured in-app browser dimensions, so exact requested 1280/390 sizes are not claimed. A measured narrow viewport was 325×703; page width about 312px and auth panel bounds 16–296px (no horizontal overflow). Label association checks passed. Default-size screenshot `.local/m5-auth-ui.jpg` was visually inspected. Full device/browser coverage remains M7.

| Required proof | Observed evidence type |
| --- | --- |
| Registration reaches Auth.Api | Automated test; live integration; manual/browser validation |
| Login reaches Auth.Api | Automated test; live integration; manual/browser validation |
| JWT stored only in sessionStorage | Automated test plus application-source review; live integration verifies stored token and absent localStorage key |
| Same-tab page refresh retains valid session | Automated restoration test; live integration new-service restoration; manual/browser full reload |
| `/api/auth/me` works with stored JWT | Automated test; live integration; browser refresh necessarily executes restoration |
| Interceptor attaches JWT to protected Auth.Api calls | Automated test; live integration observation + successful `/me` |
| Interceptor attaches JWT to Task.Api | Automated test; live integration observation + authenticated 200 |
| JWT is not attached to unrelated origin | Automated test (no external live transmission performed) |
| Anonymous `/tasks` redirects to `/login` | Automated router test; manual/browser validation |
| Logout prevents later protected navigation | Automated router test; live integration; manual/browser validation |
| Auth.Api-issued token authenticates Task.Api through Angular flow | Live integration using real AuthService/HttpClient/interceptor; automated attachment cases |

## Validation commands and exact results

Commands used bundled supported Node on PATH. Backend tests reused the existing ignored machine-specific HostLauncher unchanged.

| Command | Result |
| --- | --- |
| `npm test -- --watch=false` (regular final run) | Exit 0; 36 passed, 1 optional live test skipped; 4 files, 3 passed/1 skipped. |
| `$env:M5_LIVE='1'; npm test -- --watch=false` (both hosts running) | Exit 0; 37 passed, 0 failed/skipped; 4 files passed. |
| `npm run build` | Exit 0; production bundle 291.17 kB raw / 77.58 kB estimated transfer; no budget warnings. |
| `dotnet build TaskManager.sln --verbosity minimal` | Exit 0; all 9 projects, 0 warnings/errors after approved NuGet-config retry. |
| `dotnet test TaskManager.sln --no-restore --verbosity minimal --diag TestResults/m5-final.log -- RunConfiguration.DotNetHostPath=<historical-launcher-path>` | Exit 0; Application 65 + Infrastructure 48 + Auth.Api 23 + Task.Api 55 = 191 passed, 0 failed/skipped. |
| `git diff --check` | Exit 0; only Git line-ending notices. |
| Existing Prettier on affected Web files | Exit 0 after approved restricted-write retry. |

Initial restricted build could not read NuGet.Config; restricted Angular production build exited without diagnostics and Vite cache creation was denied. Approved retries passed. Sandboxed live hosts returned safe 500 and DataProtection access diagnostics; normal-runtime relaunch made live/browser operations pass. Validation cleanup stopped only inspected processes. No application security was weakened. Two first-run router assertions failed because they did not await logout navigation; making logout's navigation promise available and awaiting it passed. These are actual observations, not invented TDD evidence.

## Assumptions and deviations

Assumptions: canonical assessment transcription remains authoritative for agent work; short-lived sessionStorage trade-off remains accepted; standard backend HTTP local ports and explicit frontend origin match configuration; application auth responses come from trusted configured backend hosts.

No assessment/architecture/scope deviation. The prompt's conditional no-token registration path does not apply because actual M4 registration issues a token. Browser viewport measurement limitations and previously documented test runner/environment workarounds are explicitly reported; they do not change product requirements. M0–M4 history remains intact.

## Documentation and human review items

README documents both URLs, startup, routes, storage trade-off, auth/401/logout and ordinary/live tests. DEC-017 records actual choices. GENAI records actual request excerpts, output, observed corrections and evidence. All 47 traceability IDs reviewed; applicable auth/frontend rows updated with remaining M6/M7/M8 work. Personal rehearsal material at M5 described auth evidence without claiming final slides/submission; that material was subsequently removed.

Review DEC-017 and actual registration-token behavior, sessionStorage/XSS trade-off, public base URLs/CORS for the evaluator, and the opt-in probe's disposable-database requirement. Standard test-runner portability and final clean-clone/full-browser coverage remain human/M7/M8 review items. M6 owns all task UI. Validation processes were stopped after checks; no commit was created. Stop after M5.
