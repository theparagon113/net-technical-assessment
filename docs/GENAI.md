# Generative AI Development Notes

## Tools Used

Codex, operating on the repository in the desktop app. This record covers observed M2 work only; it does not reconstruct earlier milestones.

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
