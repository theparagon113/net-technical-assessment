# M8 — Submission package and presentation readiness

Publication note: this is a historical milestone record. Machine-specific executable paths in recorded commands are generalized as labeled placeholders; original scope, failures, successful retries, counts and evidence limits are preserved. Ignored launcher/runtime helpers are historical provenance, not reviewer prerequisites.

## Status and implemented scope

M8 is complete. Validation started October 1 and finished October 2, 2026 (America/Mexico_City). The repository is prepared for local reviewer setup and technical presentation. Actual GitHub publication and the candidate's rehearsal/delivered interview are human actions and are not claimed.

No product functionality, business semantics, API contract, architecture, authentication, persistence or session strategy changed. No dependency or test was added. Existing behavior/tests were preserved. No commit or push was performed, no source-repository Git remote was created/modified, and no later milestone was started.

## Files changed / purpose

| File | Purpose |
| --- | --- |
| `README.md` | Reviewer entry point: story, structure, architecture links, full setup/configuration/demo credentials, security/contracts, tests/live probes, GenAI, assumptions and limitations; removed machine-specific runner setup requirement and premature API shutdown in the example. |
| `docs/ARCHITECTURE.md` (new) | Actual component/runtime Mermaid diagram, inward compile-time dependencies, auth/task flows, claims/ownership, database initialization/seeding and verification boundaries. |
| `docs/PRESENTATION_GUIDE.md` (new) | Thirteen-section walkthrough with code to open, implementation details, likely interview questions and accurate repository-specific answers. |
| `docs/DEMO_CHECKLIST.md` (new) | Deterministic setup and five-minute seeded login/CRUD/date/session demo; disposable-data and failure contingencies. |
| `docs/PRESENTATION_NOTES.md` | Replace stale pre-task outline with final guide/navigation; distinguish preparation from human presentation. |
| `docs/REQUIREMENTS_TRACEABILITY.md` | Audit all 47 IDs, final implementation/evidence/status; preserve historical audits and explicit partial TDD adoption. |
| `docs/GENAI.md` | Final seven-deliverable evidence index, label old readiness table as checkpoint history, actual M8 request/corrections/validation; preserve real prompts/output and human corrections. |
| `docs/PROJECT_DEFINITION.md` | Final status and actual Angular organization; clarify publication is not performed by M8; preserve milestone-time context. |
| `docs/DECISIONS.md` | Add current reading guide identifying implemented/superseding decisions; no new architecture decision invented. |
| `src/frontend/task-manager-web/README.md` | Replace generic scaffold commands with actual supported scripts/config/live tests; remove unsupported `ng e2e` advice. |
| `src/frontend/task-manager-web/.vscode/launch.json` | Remove obsolete Chrome/Karma port-9876 test launch; retain development SPA launch. Vitest runs through npm. |
| `docs/M8_COMPLETION.md` (new) | This final evidence, findings and handoff report. |

Ignored validation-only helpers/results remain under `.local` and `TestResults`. They are not submission dependencies. No important local evidence was removed. Historical M3–M7 and reconciliation scope/results were preserved in M8; this later publication refinement only generalizes machine paths and condenses low-value tooling details. No separate M0–M2 completion reports exist in this checkout; earlier evidence is retained in GENAI/decisions/Git history rather than manufactured.

## Traceability and GenAI

Final matrix: **47 IDs, 46 Implemented, one Partially implemented (METH-01)**. No Planned/Missing/Not applicable rows remain. METH-01 honestly records selected observed red/green cycles plus co-authored tests/implementation; documentation cannot retroactively establish universally test-first development. Presentation rows mean the required repository explanations and demo plan are prepared, not an interview already delivered.

GenAI deliverables all have concrete recorded evidence: actual request excerpts, explicitly representative consolidated REST prompt, real controller output, validation, corrections, edge cases, auth and validation handling. Preserved examples include password-bearing diagnostics corrected with observed red/green, the human second-API requirement correction, MVC record validation metadata and reproduced mobile overflow. M8 adds actual documentation/tooling corrections without inventing product defects or human approvals.

## Clean clone and publication audit

A true local clone was created with `git clone --no-hardlinks . .local/m8-clean`, initially from HEAD `d20241582e11b5b3f4fcdbdf7124846eed760d56`. It copied tracked source/history, with no original bin/obj/node_modules/databases/runtime helpers. Dependency restoration rebuilt it from source. Final uncommitted M8 documentation/editor configuration was then overlaid for document/link review without a commit. Application/test/config/dependency files match the original checkout after normalizing Git CRLF/LF conversion. This is a local-clone check, not proof of a public remote clone or a machine without existing NuGet/npm caches.

Normal backend testing succeeded **without** the historical launcher. A subsequent historical-launcher run also passed and was unnecessary. That extra run does not turn the ignored launcher into a reviewer dependency. Both hosts and Angular were launched from the fresh clone using one disposable absolute `.local/task-manager.db` and a newly generated shared key; no previous application data/private config was required. Browser login verified exactly three seeded demo tasks before smoke mutations.

Current tracked/submission files and all 227 reachable historical blobs were scanned for private keys, common provider tokens, literal JWTs and literal signing keys, with config/source review of credential/connection settings. No secret candidates were found. No credential-bearing connection string, production signing key, local database, build output, screenshot or log is in the current submission tree. Public demo credentials and named test passwords/storage fixtures are intentionally non-production data. Tests generate ephemeral keys; application settings contain no DB/key defaults.

Root/frontend gitignore covers bin/obj, TestResults, all `.local`, databases/sidecars, node_modules/dist/Angular caches/coverage and common IDE artifacts. The three tracked frontend VS Code files are portable developer hints, not machine state; the obsolete test launch was removed. Historical Git still includes four nonempty M5 runtime paths (two output logs, process JSON and a UI image), removed from current tracking in M7. Scanning found no secret candidates there; history was preserved, not rewritten. This publication refinement generalizes machine paths in the current historical reports; older Git revisions still retain original path provenance. Current setup derives paths from the checkout. No personal application data was found in tracked source.

The source repository has **no configured Git remote**, so public GitHub availability cannot be verified. The disposable clone has Git's automatically created local origin, solely from the explicitly requested clone check; source repository remote configuration was not changed. Publication remains a human action. Human review should decide whether historical runtime artifacts and personal path provenance are acceptable before publishing the existing history.

## Recorded validation commands (paths generalized)

Commands below are the meaningful validation commands actually executed; read-only Get-Content/rg/source inspection and editing helpers are not represented as tests. `clone` below means `.local/m8-clean`; `web` means its `src/frontend/task-manager-web`. Executable placeholders represent the paths used in the original validation; they are not runnable clean-clone instructions. Use the README for reviewer commands.

```powershell
# Original repository root
dotnet --version
node --version
npm --version
dotnet restore TaskManager.sln --verbosity minimal
git clone --no-hardlinks . .local/m8-clean
git rev-parse HEAD
git remote -v

# Clone root: restore/build from fresh files
dotnet restore TaskManager.sln --verbosity minimal
dotnet build TaskManager.sln --no-restore --verbosity minimal
$env:VSTEST_CONNECTION_TIMEOUT='20'
dotnet test TaskManager.sln --no-restore --no-build --verbosity minimal

# Extra run with old local launcher, also in clone (not needed for the passing normal run)
dotnet test TaskManager.sln --no-restore --no-build --verbosity minimal --diag TestResults/m8-final.log -- RunConfiguration.DotNetHostPath=<historical-launcher-path>
dotnet list TaskManager.sln package --include-transitive --no-restore

# Clone web: initial npm wrapper run, then explicit supported runtime
$env:PATH='<supported-node-bin>;' + $env:PATH
node --version
npm ci
& '<supported-node>' '<npm-cli>' ci
& '<supported-node>' '<npm-cli>' test -- --watch=false
& '<supported-node>' '<npm-cli>' run build

# Original root: ignored helper mirrors README configuration/launch, using clone as workdir
& ./.local/m8-start.ps1
& '<supported-node>' TestResults/m7-http.mjs

# Clone web: existing opt-in live tests
$env:M5_LIVE='1'
$env:M6_LIVE='1'
& '<supported-node>' '<npm-cli>' test -- --watch=false

# Original root: final build/audit/whitespace/status
dotnet build TaskManager.sln --no-restore --verbosity minimal
& '<validation-python>' .local/m8-audit.py
git ls-files
git diff --check
git diff --cached --check
git diff --stat
git status --short
git check-ignore .local/m8-clean/.local/task-manager.db TestResults/m7-http.mjs .local/m8-audit.json
```

The audit helper uses `git ls-files --cached --others --exclude-standard -z`, `git rev-list --objects --all` and `git cat-file --batch`, scans values without printing secret payloads, checks relative Markdown links and normalized source equality. Final documentation/configuration was copied into the clone with native PowerShell `Copy-Item -LiteralPath`. Only recorded validation process trees were inspected and terminated with `taskkill /PID <recorded-id> /T /F`; other processes were untouched. Live environment variables existed only in their command shells.

## Exact results and limits

| Check | Result |
| --- | --- |
| SDK/runtime | .NET SDK 10.0.401; supported explicit Node 24.19.0; npm 11.11.0. System Node 24.14.1 is too old for the project. |
| Backend restoration | Fresh clone: all nine projects restored. Initial original-root restricted attempt could not read protected NuGet.Config; approved normal-runtime retry passed. |
| Backend build | Fresh clone and final original tree: passed, 0 warnings, 0 errors. |
| Ordinary backend tests | **194 passed**, 0 failures/skips: Application 65, SQLite Infrastructure 48, Auth HTTP 23, task/foundation/startup 58; exit 0. |
| Extra launcher suite | Same 194 passed, no failures/skips; no application/test changes. |
| Locked Angular restoration | 267 packages installed, 268 audited, **0 vulnerabilities**. Initial system npm wrapper selected old Node and warned; explicit supported-node retry passed without engine warnings. |
| Angular regular | **50 passed, 2 intentional live skips**; five files passed/two skipped; exit 0. |
| Angular live | **52 passed, no failures/skips**, all seven files; both probes used fresh-clone real hosts; exit 0. |
| Production Angular build | Passed; 314.67 kB initial raw / 82.98 kB estimated transfer, no compiler/budget warnings; exit 0. |
| Dependencies | Direct/transitive backend inventory inspected: no EF, Dapper, Mediator/MediatR or replacement ORM/mediator. No dependency change. |
| Real HTTP/security | Existing ignored M7 harness: **92 assertions passed** against M8 fresh-clone hosts; tokens not printed. |
| Browser smoke | Seeded login via Enter, three seeds, create, edit Completed, leap-day calendar persistence after refresh, delete confirmation/Cancel preserving task, logout and direct `/tasks` rejection: passed. Browser console warn/error query returned `[]`. |
| Browser layout | Measured narrow innerWidth 325/document scrollWidth 312, wide innerWidth 1066/document scrollWidth 1054; no measured overflow. Requested 390/1280 viewport overrides did not equal actual browser dimensions; screenshot capture briefly failed and later succeeded. No new exact-390/1280 claim; M7 retains exact responsive regression evidence. Override reset. |
| Browser deletion | M8 browser checked Cancel only; actual deletion is verified by both live Angular and HTTP probes. No fresh browser Confirm-delete claim. M7 historical confirmed deletion remains evidence. |
| Git checks | Working and cached `git diff --check` pass; only expected documentation/VS Code changes and four new docs. No staged changes. CRLF conversion notice is not a whitespace failure. |
| Publication scan | 126 current tracked/submission files and 227 historical blobs reviewed; no secret candidates or current runtime artifacts; no broken relative Markdown links; production snapshot matches after newline normalization. |
| Clean-clone limitation | Local HEAD clone with final uncommitted docs overlaid, existing package caches and this host environment; public remote and unrelated-machine portability were not tested. |

The live APIs retain their intentional HTTP-profile HTTPS-redirection port warning; no suppression or authentication weakening was added. The browser viewport/control errors were automation limitations: refreshed accessibility targets completed logout, and actual measured sizes are reported. Restricted process inspection initially returned access denied; approved normal-runtime inspection identified the exact validation trees before cleanup. No previously passing behavior test failed.

## Assumptions, deviations and human review

Assumptions: the canonical assessment transcription is sufficient for this agent audit; no independent original-PDF inspection is claimed. Existing accepted ports, numeric/date contracts, local HTTP demo, public demo account and short-lived sessionStorage remain valid. Validation accounts/tasks are disposable in the newly cloned database; existing user data was untouched.

No implementation deviation from the assessment, user story, established decisions or M8 scope. This is a local/public-readiness check, not actual publishing. Partial TDD adoption remains explicit. No fabricated rehearsal, public remote, tests or GenAI history is claimed.

Before publication, the human should review the documentation and small VS Code correction, review the final working-tree diff and manually include all intended submission documentation, decide whether to publish existing history containing nonsensitive old runtime artifacts/path provenance, configure/publish the public GitHub repository themselves, and rehearse the demo/code-review answers. No unresolved product defect or required failing test remains. Stop after M8.

## Final documentation-only publication refinement — October 2, 2026

This subsequent review refines public documentation without repeating M8 application validation. The recorded commands, test counts, failed/aborted attempts, successful retries and browser limits above remain historical evidence. The original assessment has since been manually cross-checked outside the repository, as clarified in ASSESSMENT_REQUIREMENTS.md; this review does not claim independent agent inspection of the proprietary PDF.

### Documents refined

| File | Publication refinement |
| --- | --- |
| `README.md` | Reduce the existing 202-line entry point to 125 lines: story, compact two-host architecture, five-step Windows PowerShell setup, visible credentials, primary tests, links and concise limitations. Document a Windows PowerShell-compatible random-key example; both API processes inherit one absolute database and one key. |
| `docs/ARCHITECTURE.md` | Retain runtime/dependency flows and add the detailed configuration/HTTP contracts, Angular state, live probes, diagnostics, HTTPS and targeted shutdown reference moved out of README. |
| `docs/ASSESSMENT_REQUIREMENTS.md` | Distinguish original transcription provenance from the subsequent manual PDF cross-check; preserve external authority without copying the PDF. |
| `docs/DECISIONS.md` | Remove unrelated interview context from the framework choice while preserving its technical rationale and DEC-015's second-API correction. |
| `docs/GENAI.md` | Replace milestone narration with labeled REST prompt excerpts/representative prompt, real code, recorded validation, correction case studies, edge cases, authentication and validation handling. Preserve partial TDD and environment/evidence limits. |
| `docs/PROJECT_DEFINITION.md` | Remove private scheduling context, describe selected versions/current validation/seed behavior precisely, and distinguish historical milestone/TDD plans from final implementation. |
| `docs/REQUIREMENTS_TRACEABILITY.md` | Align GenAI references with the refined sections and explicitly identify the four untracked submission documents. Preserve all 47 IDs/statuses. |
| `docs/PRESENTATION_NOTES.md` | Keep final guide navigation aligned with the GenAI structure and historical-preservation note. |
| `docs/PRESENTATION_GUIDE.md` | Align GenAI navigation and distinguish the historical test-host workaround from M8's ordinary fresh-clone test evidence. |
| `docs/DEMO_CHECKLIST.md` | Use a fresh disposable filename for deterministic seeds and link targeted shutdown instructions. |
| `docs/M3_COMPLETION.md`, `docs/M4_COMPLETION.md`, `docs/M5_COMPLETION.md`, `docs/M6_COMPLETION.md`, `docs/M7_COMPLETION.md`, `docs/RECONCILIATION_COMPLETION.md` | Generalize personal executable paths as explicitly labeled historical placeholders. Condense low-value editing/process-cleanup detail in M5; retain engineering corrections, failed attempts, successful retries and milestone-time scope/counts. |
| `docs/M8_COMPLETION.md` | Generalize historical runtime paths and append this separately labeled review; preserve original M8 validation facts. |

### Compliance and historical integrity

All original assessment documentation/deliverable requirements remain represented:

- User story, Clean Architecture/separation, .NET/C#, MVC/Web API, independent business validation and explicit data access.
- Task CRUD with appropriate verbs/parameters/results; a second executable authentication API with registration/login, authorized `/me` and anonymous `/public`.
- Persisted users and application data, authenticated ownership/isolation, business/data/HTTP tests and responsive Angular CRUD with clean component/state organization.
- Setup commands, preserved seeded data and visible `demo / Demo123!`; no Entity Framework, Dapper or Mediator/MediatR.
- Labeled REST generation prompt, real representative output, validation process, genuine corrections, edge cases, authentication and validation handling; presentation/design/functionality review material.

Human review's second-API correction remains explicit: it occurred after M3 and before HTTP endpoint implementation. Password diagnostic redaction, MVC metadata correction, mobile overflow and runtime-artifact tracking correction remain documented. No universal TDD history, failed-to-successful validation conversion, rehearsal or public publication is invented. METH-01 remains Partially implemented; the other 46 rows remain Implemented.

### Review checks and remaining publication actions

- `git diff --check` passed. Relative Markdown file links resolve across all 20 reviewed documents; linked heading anchors were checked. The four intended submission files `ARCHITECTURE.md`, `DEMO_CHECKLIST.md`, `M8_COMPLETION.md` and `PRESENTATION_GUIDE.md` exist at the correct relative paths but remain untracked/unstaged by explicit instruction. Links to them are not claimed to resolve to tracked files.
- Current-tree scan covered 122 tracked files plus those four documents for obvious private keys, provider tokens, literal JWT/signing keys, credential-bearing connection strings, email/phone data, personal paths and private conversational phrasing. No obvious secret/private-data candidates remained. Broader credential matches were reviewed as public demo data, synthetic test fixtures, generated configuration references, dependency names or form metadata. This is a targeted publication scan, not proof that every possible secret format is absent.
- README and architecture PowerShell blocks parsed without errors; the signing-key example generated 32 bytes without displaying or persisting the value. Ports/configuration names and sample output were checked against actual source. Full setup, application tests, browser flows and dependency audits were not rerun in this documentation pass.
- Baseline/current hashes confirm this review changed documentation only: 13 tracked Markdown files and the four authorized untracked documents. Source, tests, configuration, project/solution files and dependencies retain their entry-state bytes. Existing edits to frontend `.vscode/launch.json`, `angular.json` and its README were preserved. Consequently the complete working-tree diff includes pre-existing configuration changes; they are not attributed to this review.
- `git status --short`, `git diff --stat` and diffs were inspected. Git's tracked diff statistics include pre-existing changes and omit the four untracked documents. Nothing was staged or committed; no Git remote or history was changed.

Before submission, manually review/stage the four intended untracked documents and review the pre-existing frontend configuration edits separately. Older Git revisions still contain historical runtime artifacts and original personal path provenance; this pass scrubs current public documents without rewriting history. Public GitHub availability remains unverified because the source repository has no configured remote. Rehearse the documented setup/demo before publication.
