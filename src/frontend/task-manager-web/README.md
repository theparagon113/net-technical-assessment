# Task Manager Angular SPA

Standalone Angular 22 UI with registration/login and protected personal task CRUD. Follow the [root README](../../../README.md) for prerequisites, both API hosts, shared database/JWT setup and configuration. Separate public base URLs live in `src/app/core/api-config.ts`; signing secrets never belong here.

From this directory with supported Node/npm:

```sh
npm ci
npm start
```

Open `http://localhost:4200`. Auth.Api defaults to port 5150, Task.Api to 5149. Routes: `/login`, `/register`, protected `/tasks`. JWT-only sessionStorage restores identity through `/api/auth/me`; logout/expiry/protected 401 clears state. Forms send editable fields, numeric statuses 0/1/2 and calendar yyyy-MM-dd dates, and preserve drafts/rows on failure.

```sh
npm test -- --watch=false
npm run build
```

Regular Vitest/jsdom suite: 50 passed/two optional live skips. Production output: `dist/task-manager-web`. No browser e2e runner is configured; `ng e2e` is not supported by this project. VS Code launch opens the development SPA; tests run through npm in a terminal.

For real HttpClient/guard/service/interceptor probes, start both APIs against a disposable shared absolute database and matching JWT settings, then:

```powershell
$env:M5_LIVE = '1'
$env:M6_LIVE = '1'
try { npm test -- --watch=false }
finally { Remove-Item Env:M5_LIVE, Env:M6_LIVE }
```

Live suite: 52 passed/no skips. Probes create test accounts and mutate disposable tasks. See [M8 validation](../../../docs/M8_COMPLETION.md), [architecture](../../../docs/ARCHITECTURE.md), [presentation](../../../docs/PRESENTATION_GUIDE.md) and [demo](../../../docs/DEMO_CHECKLIST.md). Historical results remain in M5–M7 reports.
