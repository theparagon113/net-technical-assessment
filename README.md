# Task Manager

A .NET 10 / C# and Angular 22 application for registering, signing in and managing personal tasks. Tasks have a title, optional description, status and calendar due date; users can access only their own records.

## User Story

> As a registered user, I want to securely manage my personal tasks so that I can keep track of the work I need to complete.

## Architecture

Two separate executable ASP.NET Core MVC / Web API hosts reuse the same Domain, Application and Infrastructure layers and one SQLite database:

```text
Angular
   +--> TaskManager.Auth.Api --> shared SQLite database
   |       registration / login / anonymous public / authorized me
   |
   +--> TaskManager.Api ------> shared SQLite database
           JWT-protected task CRUD
```

Application services and Domain invariants handle business rules; Infrastructure uses Microsoft.Data.Sqlite, parameterized SQL and explicit mapping. No Entity Framework, Dapper or Mediator/MediatR is used. See the [architecture reference](docs/ARCHITECTURE.md) for dependencies, request flows and HTTP contracts.

## Quick Start — Windows PowerShell

**Prerequisites:** Git, .NET 10 SDK (the backend targets `net10.0`), and Node.js with npm (project version: 11.11.0). Recommend [Node.js 24 LTS](https://nodejs.org/en/about/previous-releases); when using Node 24, **24.15.0 or newer within the 24.x series is required**. The full supported Node range in `package.json` is `^22.22.3 || ^24.15.0 || >=26.0.0`. Dependency restoration needs NuGet/npm access. SQLite requires no database server.

Install prerequisites if needed:
- .NET 10 SDK: https://dotnet.microsoft.com/download/dotnet/10.0
- Node.js 24 LTS: https://nodejs.org/
- Git: https://git-scm.com/downloads

1. Open PowerShell **at the cloned repository root**. Verify prerequisites, restore and build:

   ```powershell
   dotnet --version
   node --version
   npm --version
   dotnet restore TaskManager.sln
   dotnet build TaskManager.sln
   ```

2. In that same shell, create local storage and configure **one absolute SQLite file and one JWT key shared by both APIs**. Run this block once per demo session:

   ```powershell
   $taskManagerDataDirectory = Join-Path (Get-Location).Path '.local'
   New-Item -ItemType Directory -Force -Path $taskManagerDataDirectory | Out-Null
   $taskManagerDatabaseFile = Join-Path $taskManagerDataDirectory 'task-manager.db'
   $env:ConnectionStrings__TaskManager = "Data Source=$taskManagerDatabaseFile"
   $env:Jwt__Issuer = 'task-manager-local'
   $env:Jwt__Audience = 'task-manager-backend'
   $taskManagerKeyBytes = New-Object byte[] 32
   $taskManagerRng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
   try { $taskManagerRng.GetBytes($taskManagerKeyBytes) }
   finally { $taskManagerRng.Dispose() }
   $env:Jwt__SigningKey = [Convert]::ToBase64String($taskManagerKeyBytes)
   $env:Jwt__LifetimeMinutes = '15'
   $env:Cors__FrontendOrigin = 'http://localhost:4200'
   ```

   Keep the signing key outside source control; do not put it in `appsettings.json` or Angular. Both hosts reject missing/relative database paths. Do not generate a separate key for each host.

3. Start **Auth.Api first** for demo population, then Task.Api, from the same configured shell. Both processes inherit the exact database/JWT settings:

   ```powershell
   $taskManagerAuthProcess = Start-Process dotnet -WindowStyle Hidden -PassThru `
       -ArgumentList 'run --no-build --project src/backend/TaskManager.Auth.Api --launch-profile http' `
       -RedirectStandardOutput (Join-Path $taskManagerDataDirectory 'auth-output.log') `
       -RedirectStandardError (Join-Path $taskManagerDataDirectory 'auth-error.log')
   $taskManagerTaskProcess = Start-Process dotnet -WindowStyle Hidden -PassThru `
       -ArgumentList 'run --no-build --project src/backend/TaskManager.Api --launch-profile http' `
       -RedirectStandardOutput (Join-Path $taskManagerDataDirectory 'task-output.log') `
       -RedirectStandardError (Join-Path $taskManagerDataDirectory 'task-error.log')
   Get-Content (Join-Path $taskManagerDataDirectory 'auth-output.log')
   Get-Content (Join-Path $taskManagerDataDirectory 'task-output.log')
   ```

   Wait until both logs show `Now listening on`; repeat the two `Get-Content` commands if needed. Auth.Api uses **http://localhost:5150**, Task.Api **http://localhost:5149**. Inspect the corresponding error logs if startup fails. Both hosts automatically create the SQLite file and initialize schema; no manual SQL, migrations or separate database setup command is required. Only Auth.Api seeds three tasks for a newly created demo account. Restarts preserve existing credentials, task edits and deletions. Task.Api can start independently but does not seed.

4. Keep both APIs running. In a **second PowerShell terminal at the repository root**, start Angular:

   ```powershell
   Set-Location src/frontend/task-manager-web
   npm ci
   npm start
   ```

   `npm ci` installs the locked dependencies, including the local Angular CLI; no global Angular CLI installation is required. The frontend already targets Auth.Api on port 5150 and Task.Api on port 5149 in `src/app/core/api-config.ts`. The documented HTTP profiles require no HTTPS development certificate or frontend configuration changes. Keep ports 4200, 5150 and 5149 available.

5. Open **[http://localhost:4200](http://localhost:4200)** and sign in:

   | Username | Password |
   | --- | --- |
   | `demo` | `Demo123!` |

   These are intentionally public demo credentials. An existing demo account retains its current password; use a new filename in step 2 for fresh seed data. Create, edit, change status and delete tasks, then sign out.

   Alternatively, choose **Create an account** on the login page (or open [http://localhost:4200/register](http://localhost:4200/register)). Registration signs you in automatically; a new account starts with an empty task list.

For shutdown, HTTPS profiles, HTTP probes and troubleshooting, see [local runtime details](docs/ARCHITECTURE.md#local-runtime-reference).

## Running tests

In a separate PowerShell terminal at the repository root, run backend tests. They provide isolated databases and JWT settings; neither running API processes nor the demo environment variables are required:

```powershell
dotnet test TaskManager.sln
```

From that same repository-root terminal, after `npm ci` in Quick Start step 4, run frontend tests:

```powershell
Set-Location src/frontend/task-manager-web
npm test -- --watch=false
```

From the frontend directory, build for production (`angular.json` defaults to the production configuration):

```powershell
npm run build
```

Tests cover Application/business behavior, real SQLite access, both HTTP pipelines, authentication and user isolation, plus Angular forms, routing and CRUD. Regular Angular tests skip two opt-in live cases; [live-probe instructions](docs/ARCHITECTURE.md#live-angular-probes) use a disposable database. Recorded M8 results: 194 backend passes, 50 regular Angular passes and 52 with both live probes enabled. These are historical results, documented in the final validation report.

## Documentation

- [Assessment requirements](docs/ASSESSMENT_REQUIREMENTS.md) and [project definition](docs/PROJECT_DEFINITION.md)
- [Architecture and HTTP contracts](docs/ARCHITECTURE.md)
- [GenAI prompt, generated output, validation and corrections](docs/GENAI.md)
- [Design decisions](docs/DECISIONS.md)
- [Requirements traceability](docs/REQUIREMENTS_TRACEABILITY.md)
- [Final validation and publication review](docs/M8_COMPLETION.md)

## Limitations / trade-offs

SQLite suits this small workload; schema initialization is not a general migration system. The short-lived JWT in sessionStorage is accessible under XSS, and logout does not revoke a copied token. No refresh tokens, password recovery, sharing, pagination or realtime concurrency handling are implemented. HTTP profiles are for local development; no deployment or production-readiness claim is made.

M0–M8 implementation is complete. TDD adoption was partial: selected failing/passing cycles are recorded, alongside co-authored implementation/tests. [Decisions](docs/DECISIONS.md) and [GenAI evidence](docs/GENAI.md) explain these trade-offs. Public publication and human rehearsal/presentation remain submission actions.
