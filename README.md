# Task Manager

A personal task manager developed for a .NET full-stack technical assessment.

M0–M5 and the post-M3 reconciliation checkpoint are complete. The backend has two independent controller-based ASP.NET Core hosts with authentication, task CRUD, shared SQLite persistence, and HTTP integration tests. Angular implements authentication and a protected session placeholder. Task management UI belongs to M6; full-system hardening and the final submission/presentation remain incomplete.

## Stack and architecture

- .NET 10 / ASP.NET Core MVC Web API controllers / C#
- SQLite through `Microsoft.Data.Sqlite`, explicit parameterized SQL and mapping
- Framework password hashing and JWT Bearer authentication
- xUnit, real SQLite tests, and `WebApplicationFactory` HTTP tests
- Angular 22 / TypeScript / standalone components / Router / CSS

```text
Domain ← Application ← Infrastructure
                          ↑       ↑
               TaskManager.Api   TaskManager.Auth.Api
                   task CRUD     register/login/public/me
                          \       /
                          one SQLite file
```

Neither host references or calls the other. Application rules and ownership checks remain in the existing inner layers. `src/backend/SharedApi` is linked presentation-layer source compiled into both hosts for consistent composition, errors, claims and CORS; it is not another project/layer. JWT validation policy lives in Infrastructure/Authentication. Controllers are grouped by feature.

See [assessment requirements](docs/ASSESSMENT_REQUIREMENTS.md), [project definition](docs/PROJECT_DEFINITION.md), [user story](docs/USER_STORY.md), [decisions](docs/DECISIONS.md), [traceability](docs/REQUIREMENTS_TRACEABILITY.md), and [M4 completion report](docs/M4_COMPLETION.md).

## Local backend setup

Install the .NET 10 SDK. Run from the repository root. Supply one absolute SQLite file and identical JWT configuration to both hosts. There are no tracked database/signing-key defaults.

```powershell
dotnet restore TaskManager.sln
dotnet build TaskManager.sln

$taskManagerDataDirectory = Join-Path (Get-Location).Path '.local'
New-Item -ItemType Directory -Force -Path $taskManagerDataDirectory | Out-Null
$taskManagerDatabaseFile = Join-Path $taskManagerDataDirectory 'task-manager.db'
$env:ConnectionStrings__TaskManager = "Data Source=$taskManagerDatabaseFile"
$env:Jwt__Issuer = 'task-manager-local'
$env:Jwt__Audience = 'task-manager-backend'
$env:Jwt__SigningKey = [Convert]::ToBase64String(
    [System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
$env:Jwt__LifetimeMinutes = '15'
$env:Cors__FrontendOrigin = 'http://localhost:4200'

# Both processes inherit this exact configuration. Generate the signing key once.
$taskManagerAuthProcess = Start-Process dotnet -WindowStyle Hidden -PassThru `
    -ArgumentList 'run --no-build --project src/backend/TaskManager.Auth.Api --launch-profile http' `
    -RedirectStandardOutput (Join-Path $taskManagerDataDirectory 'auth-output.log') `
    -RedirectStandardError (Join-Path $taskManagerDataDirectory 'auth-error.log')
$taskManagerTaskProcess = Start-Process dotnet -WindowStyle Hidden -PassThru `
    -ArgumentList 'run --no-build --project src/backend/TaskManager.Api --launch-profile http' `
    -RedirectStandardOutput (Join-Path $taskManagerDataDirectory 'task-output.log') `
    -RedirectStandardError (Join-Path $taskManagerDataDirectory 'task-error.log')
```

Auth HTTP is `http://localhost:5150`; task HTTP is `http://localhost:5149`. Inspect the local output/error logs for startup. To run interactively instead, use the corresponding `dotnet run` commands in terminals configured with the exact same values. HTTPS profiles retain auth port 7139 and task port 7138; use trusted local development certificates when choosing those profiles. The HTTP profiles are for local assessment development.

Both hosts ensure the transactional/idempotent schema and deliberately create the database's parent directory. Task.Api can start first and does not seed. Auth.Api hashes the public demo password through `IPasswordHasher` and seeds only a newly inserted demo account. Reruns do not reset existing credentials, overwrite task edits or restore deletions, including existing demo usernames in other casing. Start Auth.Api for demo population; Task.Api has no startup HTTP dependency on it.

After startup, exercise the actual login and protected task APIs:

```powershell
$taskManagerLogin = Invoke-RestMethod -Method Post `
    -Uri 'http://localhost:5150/api/auth/login' -ContentType 'application/json' `
    -Body (@{ username = 'demo'; password = 'Demo123!' } | ConvertTo-Json)
$taskManagerHeaders = @{ Authorization = "Bearer $($taskManagerLogin.accessToken)" }
Invoke-RestMethod -Uri 'http://localhost:5150/api/auth/me' -Headers $taskManagerHeaders
Invoke-RestMethod -Uri 'http://localhost:5149/api/tasks' -Headers $taskManagerHeaders

# Stop only these locally launched hosts when finished.
Stop-Process -Id $taskManagerAuthProcess.Id, $taskManagerTaskProcess.Id
```

Demo credentials are **demo / Demo123!**, intentionally public assessment data. An existing demo account keeps its existing password.

## Configuration and security

| Setting (environment variable) | Behavior |
| --- | --- |
| `ConnectionStrings:TaskManager` (`ConnectionStrings__TaskManager`) | Required valid SQLite connection string with absolute file Data Source; same file for both processes. Missing/empty/relative/in-memory host configuration fails startup. |
| `Jwt:Issuer` (`Jwt__Issuer`) | Required nonblank issuer, identical in both hosts. |
| `Jwt:Audience` (`Jwt__Audience`) | Required nonblank logical backend audience, identical in both hosts. |
| `Jwt:SigningKey` (`Jwt__SigningKey`) | Required Base64 random key of at least 32 bytes. Never commit it. |
| `Jwt:LifetimeMinutes` (`Jwt__LifetimeMinutes`) | Default 15; permitted 1–60. |
| `Cors:FrontendOrigin` (`Cors__FrontendOrigin`) | Default `http://localhost:4200`; one explicit HTTP(S) origin, no unrestricted origins. |

Tokens use HS256, `sub` (positive integer user ID), `unique_name` (display username), issuer/audience, not-before and expiration. Both hosts validate signature, algorithm, issuer, audience and lifetime with zero clock skew and disabled claim remapping. Missing, repeated, malformed or nonpositive subjects fail authentication. Task ownership always comes from the validated identity. Extra ownership fields in JSON/query are ignored and cannot change the owner. No refresh tokens are implemented.

Usernames are trimmed, allow 3–64 UTF-16 characters excluding control characters, preserve display casing, and compare using `StringComparer.OrdinalIgnoreCase`. SQLite registers the shared `USERNAME_IDENTITY` collation and enforces its unique index. Existing identity collisions fail initialization transactionally; external SQL tools must register the same collation to write usernames. Passwords allow 8–128 UTF-16 characters, reject whitespace-only input, and are never trimmed. The framework IdentityV3 hasher uses salted PBKDF2-HMAC-SHA512 with 210,000 iterations.

Every repository opens/disposes its own connection and enforces foreign keys. Resource queries include both task ID and user ID. Due dates are calendar dates stored as invariant `yyyy-MM-dd`, with no time zone. The connection factory still supports isolated caller-supplied test configurations; absolute file restrictions apply to the HTTP hosts.

## HTTP contract

| Host | Endpoint | Success | Errors |
| --- | --- | --- | --- |
| Auth.Api | `POST /api/auth/register` | 201 with user ID, username, access token and UTC expiration | 400 input; 409 duplicate |
| Auth.Api | `POST /api/auth/login` | 200 with same authentication result | 400 input; generic 401 credentials |
| Auth.Api | `GET /api/auth/public` | Anonymous 200 with small public message | — |
| Auth.Api | `GET /api/auth/me` | Protected 200 with user ID/display username | 401 authentication |
| Task.Api | `GET /api/tasks` | Protected 200 with owned tasks; empty collection is `[]` | 401 authentication |
| Task.Api | `GET /api/tasks/{id}` | Protected 200 with owned task | 400 malformed ID; 401; 404 |
| Task.Api | `POST /api/tasks` | Protected 201 with task and GET-by-ID Location | 400; 401 |
| Task.Api | `PUT /api/tasks/{id}` | Protected 200 with updated task | 400; 401; 404 |
| Task.Api | `DELETE /api/tasks/{id}` | Protected 204 | 400; 401; 404 |

Missing and inaccessible tasks return the same 404. Errors use safe ProblemDetails, including unexpected 500 responses; login never distinguishes unknown username from wrong password. Responses never include passwords, password hashes or signing keys. Registration returns 201 without inventing a user resource Location.

Task requests contain `title`, optional `description`, required `dueDate`, and optional numeric `status` (0 Pending, 1 InProgress, 2 Completed; default Pending). `dueDate` is the JSON spelling of the assessment's `due_date` calendar field. Results also contain server-assigned `id` and `userId`. Titles are trimmed/required with maximum 120 characters; descriptions maximum 1000. Past dates are allowed. Example request:

```json
{"title":"Review assessment","description":"Check requirements","dueDate":"2026-10-03","status":0}
```

## Validation

```powershell
dotnet build TaskManager.sln
dotnet test TaskManager.sln
dotnet list TaskManager.sln package --include-transitive --no-restore
git diff --check
```

Application tests use fast abstractions; Infrastructure tests use actual isolated SQLite. Both API suites exercise routing/model binding, JWT authentication, authorization, CORS, errors, controllers, services and real SQLite through `WebApplicationFactory`. Task tests obtain tokens from actual Auth.Api registration/login HTTP responses and use them in the independent task HTTP pipeline. Startup tests cover different content roots, Task.Api first/no seed, Auth.Api seeding, concurrent/repeated initialization, and credential/edit/deletion preservation.

On this development machine only, the existing loopback testhost redirection requires the ignored local HostLauncher described in [GenAI evidence](docs/GENAI.md). The recorded M4 full-suite command is:

```powershell
dotnet test TaskManager.sln --no-restore --verbosity minimal --diag TestResults/m4-final.log -- RunConfiguration.DotNetHostPath=C:/Maethrillian/NET-TechnicalAssessment/TestResults/HostLauncher/bin/Debug/net10.0/HostLauncher.exe
```

That local launcher is not a clean-clone prerequisite or application dependency. Standard test portability on this machine is not claimed.

## Angular authentication (M5)

From `src/frontend/task-manager-web`, use supported Node `^22.22.3`, `^24.15.0` or `>=26.0.0`:

```sh
npm ci
npm start
npm run build
npm test -- --watch=false
```

Open `http://localhost:4200`. Start both backend hosts using the shared configuration above. Central public configuration is `src/frontend/task-manager-web/src/app/core/api-config.ts`: `authApiBaseUrl` defaults to `http://localhost:5150`, and `taskApiBaseUrl` to `http://localhost:5149`. Set each to its own host base URL without a trailing slash. For a different frontend origin, update `Cors__FrontendOrigin` in both hosts. Backend signing secrets never belong in frontend configuration.

`/login` and `/register` use Reactive Forms and Auth.Api's actual `{ username, password }` request and `{ userId, username, accessToken, expiresAt }` response. Both successful operations establish a session and navigate to protected `/tasks`, which is an authentication placeholder only. Registration already issues a token in M4; no hidden login is performed. Logout clears the session and navigates to `/login`.

Only the JWT is persisted under `task-manager.access-token` in sessionStorage. A same-tab refresh rejects malformed/expired tokens and confirms safe identity through protected Auth.Api `/api/auth/me` before allowing navigation. An expiry timer and protected-request 401 clear stale state; login 401 remains a generic form error. Requests time out after ten seconds. The interceptor attaches Bearer only to configured Auth.Api `/api/auth/me` and Task.Api `/api/tasks` or child paths, with exact origin/path boundaries; unrelated origins/assets and anonymous auth endpoints receive no token.

sessionStorage remains accessible to JavaScript under XSS, but limits persistence compared with localStorage. A production architecture could use secure HttpOnly cookies/BFF. This assessment introduces no refresh tokens; guards improve UX and the backend remains the authorization boundary.

The normal test suite uses HTTP mocks and skips one opt-in live test. For real Angular HttpClient/AuthService/interceptor verification, launch both hosts against a **disposable absolute SQLite file** (the probe creates a test user) and matching JWT configuration, then run:

```powershell
$env:M5_LIVE = '1'
npm test -- --watch=false
Remove-Item Env:M5_LIVE
```

The live probe registers/logs in through Auth.Api, confirms `/me`, calls Task.Api's protected collection through the real interceptor, restores a new service from stored JWT, and verifies logout/route protection. It adds no production task feature. See [M5 completion/evidence](docs/M5_COMPLETION.md) for exact results, browser checks and environment limitations. Task UI (M6), final full-system hardening (M7), clean-clone review and presentation (M8) remain.
