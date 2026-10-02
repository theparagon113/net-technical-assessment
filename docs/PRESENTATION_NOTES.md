# Presentation Preparation (pending final M8 presentation)

These are preparation notes, not a completed presentation or functioning demo.

1. Include the informal developer user story from USER_STORY.md verbatim and explain the user's goal: securely managing personal tasks.
2. Show both separate executable API hosts: TaskManager.Auth.Api owns register/login/public/current-user endpoints; TaskManager.Api owns task CRUD. Explain the assessment's explicit SECOND API wording and the human review that corrected the original AI-assisted single-host plan before endpoints existed.
3. Explain Clean Architecture: shared Domain, Application, Infrastructure; two outer presentation hosts; focused services and repository ports; business rules independent from HTTP/SQLite. No duplicate layers/microservice infrastructure.
4. Explain persistence: Microsoft.Data.Sqlite, explicit parameterized SQL, owner predicates, Users/Tasks in one database, required common absolute path, idempotent initialization, Auth.Api-only seed startup, preservation of existing credentials/tasks.
5. Explain security: framework salted hashing, consistent username identity/unique index/races, JWT issuing in Auth.Api and compatible validation in both hosts, claims as owner authority, no secrets committed, no refresh tokens.
6. Explain testing/TDD honestly: existing business unit and real SQLite suites, observed red/green evidence and exceptions in GENAI.md, implemented M4 HTTP-pipeline/cross-host tests; no fabricated test-first history.
7. Demonstrate GenAI fluency: actual prompt, actual REST API output (recorded in GENAI.md for M4), how suggestions were validated, real redaction/second-host corrections, edge cases, auth and validation handling.
8. Live functionality demo (backend M4 and Angular auth M5 implemented; task UI pending M6): run both hosts and Angular with two base URLs; show public endpoint, registration/login, protected current user and logout. Full seeded task CRUD/status/due date, validation and second-user task UI isolation demo follows M6.
9. Explain familiar Angular choices and M5 auth responsiveness/usability/state organization evidence. Complete full-task browser/console review in M6/M7.
10. Final readiness (M8): validate clean-clone instructions and demo credentials, include design choices/architecture/functionality in presentation, rehearse review questions, verify every applicable traceability row with evidence.

Current evidence: M4 provides actual API prompt/output and HTTP tests. M5 provides Angular authentication, real two-host frontend forwarding and browser auth evidence. Final slides/talk/full-task demo, full-system/browser hardening and clean-clone review remain pending. This outline is not final presentation delivery.

## M5 authentication preparation

Show two public base URLs: auth calls reach Auth.Api; task calls reach Task.Api. Registration already returns a JWT, so the UI establishes its returned session directly. Explain sessionStorage/XSS trade-off, no refresh tokens, authoritative /me restoration, exact origin/path Bearer scoping, login 401 versus protected-session rejection, guard as UX and backend as authorization boundary.

M5 evidence: 36 regular Angular cases plus one passing opt-in real-host probe; 191 backend regression cases. Browser registration, Enter login, generic credential error, same-tab refresh, logout and post-logout protection were observed. Auth screenshots/bounds and label/current-console checks exist, with viewport limitations in M5_COMPLETION.md. The /tasks page is only a session placeholder; task CRUD UI remains M6. Task.Api forwarding proof is isolated test code using real AuthService/HttpClient/interceptor.
