# Live demo checklist

Allow roughly five minutes for the demo after setup. Use the root [README setup](../README.md) from a fresh checkout; the commands derive the absolute database path and generate one random key inherited by both hosts. Never display the key or JWT in the presentation.

## Before the interview

1. Restore/build with `dotnet restore TaskManager.sln` and `dotnet build TaskManager.sln` from the root. In `src/frontend/task-manager-web`, run `npm ci`. Use a supported Node version from README/package.json.
2. Run the README's PowerShell configuration and hidden Auth.Api/Task.Api launch block once in the same shell. Before assigning the connection string, choose a fresh disposable filename: `$taskManagerDatabaseFile = Join-Path $taskManagerDataDirectory ('interview-demo-{0}.db' -f [Guid]::NewGuid().ToString('N'))`. This keeps both hosts on one absolute file and guarantees fresh demo seeds without resetting existing data. Auth.Api owns seeding.
3. Wait for the startup messages in `.local/auth-output.log` and `.local/task-output.log`. Verify anonymous `Invoke-RestMethod http://localhost:5150/api/auth/public` returns the public message. Auth is port 5150; tasks 5149.
4. In a separate terminal: `Set-Location src/frontend/task-manager-web`, then `npm start`. Open `http://localhost:4200`. Keep both APIs running.
5. Open the presentation guide and the code files listed there. Rehearse once. Do not run live test probes against the demo database; use a separate disposable database for validation.

## On screen

| Step | Action | Expected observation / explanation |
| --- | --- | --- |
| 1 | Open `/tasks` anonymously | Redirect to `/login`; API authorization is independently tested. |
| 2 | Log in as `demo / Demo123!` using Enter | Three seeded tasks on a fresh database. Existing accounts retain their existing password/data. |
| 3 | Create `Interview task`, description `Calendar deadline`, Pending, due date `2024-02-29` | Saved task appears; past calendar dates are valid. |
| 4 | Edit title to `Interview task reviewed`, status In progress, keep date | Saved values appear; enum value is 1. |
| 5 | Edit status to Completed | Saved value is 2; no separate completion endpoint. |
| 6 | Refresh the same browser tab | `/me` restores identity; saved task/date remain unchanged, with no timezone conversion. |
| 7 | Click Delete, then Cancel | Task remains. |
| 8 | Delete that disposable interview task and confirm | Success feedback; row disappears only after server 204. Seeded tasks remain. |
| 9 | Log out, then open `/tasks` | Login screen; token cleared and guarded navigation blocked. |
| 10 | Optional: register a unique interview username | Successful registration directly establishes a session; new account has an empty owned collection. No hidden login. |

Explain ownership using `TaskHttpTests.Auth_login_token_authenticates_task_CRUD_and_enforces_cross_user_isolation` and the real HTTP evidence rather than modifying seeded records. Explain failed-write preservation/404 reload recovery using the tests and M7/M8 evidence; do not deliberately interrupt servers during the short demo.

## Finish / contingency

Sign out. Press Ctrl+C in the Angular terminal and follow the [targeted API shutdown commands](ARCHITECTURE.md#local-runtime-reference) using the process objects from the README launch block. Keep the disposable database ignored; restarting does not reset it. If localhost ports are occupied, stop your own previous demo processes or use the documented configuration/base URLs consistently; never terminate an unknown process.

If startup/login fails, inspect local logs and matching database/JWT configuration. A newly generated key invalidates previous tokens: sign out and log in again. If an existing demo account has changed credentials, use a new database path rather than resetting it. If a write times out, reload to see whether it reached the server before retrying. Automated evidence remains available if live demonstration is unavailable.
