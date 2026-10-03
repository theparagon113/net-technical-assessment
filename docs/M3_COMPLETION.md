# M3 Completion Report

Publication note: this is a historical milestone record. Machine-specific executable paths in recorded commands are generalized as labeled placeholders; original scope, failures, successful retries, counts and evidence limits are preserved. Ignored launcher/runtime helpers are historical provenance, not reviewer prerequisites.

## Implemented

M3 implements registration/login application behavior, immutable input/result/token contracts, validation policies, focused authentication failures, supported password hashing/verification, configurable signed JWT creation, definitive case-insensitive username identity, safe M2 index migration, and usable demo hashing/seeding pieces. API/startup composition remains M4. The initial working tree was clean; M0/M1 task behavior and all prior database tests remain intact. No commit was created.

## Files Created

Paths below are repository-relative.

| File | Purpose |
| --- | --- |
| `src/backend/TaskManager.Application/Authentication/AuthInput.cs` | Immutable username/password input; redacted diagnostic string |
| `src/backend/TaskManager.Application/Authentication/AuthResult.cs` | Safe user identity/token/expiry result; token-redacted diagnostic string |
| `src/backend/TaskManager.Application/Authentication/AccessToken.cs` | Token and UTC expiry contract; redacted diagnostic string |
| `src/backend/TaskManager.Application/Authentication/AuthService.cs` | Validated registration/login flows through application abstractions |
| `src/backend/TaskManager.Application/Authentication/IPasswordHasher.cs` | Hash/verify boundary |
| `src/backend/TaskManager.Application/Authentication/ITokenService.cs` | Access-token creation boundary |
| `src/backend/TaskManager.Application/Authentication/DuplicateUsernameException.cs` | Same duplicate outcome for availability checks and insert races |
| `src/backend/TaskManager.Application/Authentication/InvalidCredentialsException.cs` | One login failure type/message |
| `src/backend/TaskManager.Application/Authentication/UsernamePolicy.cs` | Username validation, trimming, centralized ordinal case-insensitive comparison |
| `src/backend/TaskManager.Application/Authentication/PasswordPolicy.cs` | Deterministic bounded password validation |
| `src/backend/TaskManager.Infrastructure/Authentication/FrameworkPasswordHasher.cs` | IdentityV3 salted adaptive hashing/verification |
| `src/backend/TaskManager.Infrastructure/Authentication/JwtOptions.cs` | External issuer/audience/key/lifetime configuration |
| `src/backend/TaskManager.Infrastructure/Authentication/JwtTokenService.cs` | Validated HS256 JWT creation with testable clock |
| `tests/TaskManager.Application.Tests/AuthServiceTests.cs` | Registration/login, validation, casing, failures, race, cancellation, diagnostic safety |
| `tests/TaskManager.Infrastructure.Tests/AuthenticationTests.cs` | Real hashing/JWT validation, SQLite authentication, real insert race, demo login/idempotence |
| `tests/TaskManager.Infrastructure.Tests/UsernameIdentityTests.cs` | Direct SQL uniqueness, repository identity, Unicode, migration/collision rollback, uppercase demo preservation |
| `docs/M3_COMPLETION.md` | This reviewable milestone report |

## Files Changed

| File | Purpose |
| --- | --- |
| `src/backend/TaskManager.Application/Users/IUserRepository.cs` | Document identity and duplicate-race contract |
| `src/backend/TaskManager.Domain/User.cs` | Update stale hashing-deferral comment; preserve model behavior |
| `src/backend/TaskManager.Infrastructure/Persistence/SqliteConnectionFactory.cs` | Register shared username collation on each connection |
| `src/backend/TaskManager.Infrastructure/Persistence/SqliteDatabaseInitializer.cs` | Transactionally install unique identity index; reject legacy collisions without data changes |
| `src/backend/TaskManager.Infrastructure/Persistence/Repositories/SqliteUserRepository.cs` | Case-insensitive lookup and unique-violation translation |
| `src/backend/TaskManager.Infrastructure/Persistence/SqliteDemoSeeder.cs` | Local credential constants and conflict handling for both username constraints |
| `src/backend/TaskManager.Infrastructure/TaskManager.Infrastructure.csproj` | ASP.NET shared framework reference and Microsoft JWT package 8.19.2 |
| `tests/TaskManager.Infrastructure.Tests/SqlitePersistenceTests.cs` | Expect new duplicate application exception; retain preservation coverage |
| `README.md` | M3 status, configuration, policies, migration, local demo strategy |
| `docs/DECISIONS.md` | DEC-012 username supersession and DEC-013 cryptography/configuration |
| `docs/GENAI.md` | Actual prompt excerpts, output, validation, corrections, and TDD/tooling limitations |

Temporary launcher source/binaries and runner diagnostics under ignored `TestResults/` are local validation artifacts, not application changes or new solution projects. No API or frontend sources, project reference directions, task repositories, or task service behavior changed.

## Authentication Design

AuthService depends only on IUserRepository, IPasswordHasher, and ITokenService. Registration validates, trims display username, checks identity availability, hashes the unmodified password, persists an unsaved User, then creates a token. Repository uniqueness races map to DuplicateUsernameException. Login validates input, performs the same identity lookup, verifies the stored hash, and issues tokens only on success. Both nonexistent-user and wrong-password failures produce InvalidCredentialsException with `Invalid username or password.` Results expose only user ID, display username, access token, and UTC expiration. Cancellation tokens reach all asynchronous repository calls; canceled flows do not issue tokens.

## Username Policy and Database Enforcement

The exact identity comparison is `StringComparer.OrdinalIgnoreCase.Compare(left?.Trim(), right?.Trim())`, centralized in UsernamePolicy. Validation permits 3–64 UTF-16 characters after trimming and rejects controls/blank input. There is no culture-dependent lowercasing, accent folding, or canonical Unicode normalization. Trimmed original casing is persisted and returned for display: registering ` Daniel ` displays `Daniel` on later lowercase/uppercase login.

SqliteConnectionFactory registers this comparison as USERNAME_IDENTITY. Initialization adds unique index IX_Users_UsernameIdentity on Username using that collation, and username lookup explicitly selects the same collation. Existing case-sensitive uniqueness remains redundant; no table rebuild is needed. Initialization is transactional and preserves M2 data. Existing case/trim-equivalent legacy accounts cause an explicit failure and rollback; automatic deletion/merging would risk account ownership.

Explicit evidence:

- `Database_rejects_case_equivalent_insert_without_auth_service` directly inserts `daniel`, `DANIEL`, `danIEL`, and ` Daniel ` after `Daniel` and asserts SQLite unique error 2067, bypassing AuthService and repository duplicate mapping.
- `Real_sqlite_registration_login_and_duplicate_behavior` registers ` Daniel `, successfully logs in as `daniel`, `DANIEL`, and ` DaNiEl ` with real SQLite and framework hashing, preserves display `Daniel`, and rejects duplicate registration.
- `Real_insert_race_maps_to_duplicate_without_issuing_token` verifies a competing lowercase insert after availability lookup produces the same application failure without token generation.
- Unicode identity tests verify the shared comparison for accented and Turkish-cased examples. Migration tests cover retained IDs/hashes/display names, repeated initialization, and collision rollback. Original M2 tests continue to verify foreign keys and task preservation.

## Password Hashing, JWT, and Demo

FrameworkPasswordHasher uses the supported PasswordHasher<object>, IdentityV3, salted PBKDF2-HMAC-SHA512 at 210,000 iterations. Passwords allow 8–128 UTF-16 characters, reject whitespace-only input, and are never trimmed. Framework verification accepts valid hashes, rejects wrong passwords, and treats malformed legacy hash data as failure. Same-password hashing produces different salted hashes. Framework-specific types remain in Infrastructure; there is no full Identity persistence/ORM system.

JwtTokenService uses Microsoft's System.IdentityModel.Tokens.Jwt 8.19.2, HS256, and claims sub (stable integer ID), unique_name, iss, aud, nbf, exp. IOptions<JwtOptions> supplies nonblank issuer/audience, a Base64 random key containing at least 32 bytes, and lifetime (default 15 minutes; allowed 1–60). Invalid configuration fails construction. Tests validate the signature and reject wrong signing keys, issuer, audience, and expired tokens. No secret is committed; tests generate random ephemeral keys. No refresh tokens are implemented.

The local assessment account is **demo / Demo123!**, intentionally public demo data. Later startup composition initializes the database, hashes SqliteDemoSeeder.Password through IPasswordHasher, then calls SeedAsync with the hash. The real SQLite demo test verifies login, preserved hash, and preserved task deletion across reruns. Existing DEMO accounts receive no new tasks/password reset. Startup wiring remains M4, so no HTTP demo login is claimed yet.

## Tests Added or Updated

20 new Application cases and 22 new Infrastructure cases were added. One existing M2 test now asserts the application duplicate exception required by the new contract; direct SQL tests separately retain verification of SQLite error 2067. All original 45 Application and 26 Infrastructure cases continue to pass.

The initial AuthService tests were authored with implementation, not represented as TDD. SQLite identity tests preceded persistence changes, but test-host failures prevented verified red evidence. The security correction for password diagnostic output has an observed failing test followed by a passing test; see GENAI.md.

## Validation Performed

| Command | Result |
| --- | --- |
| `dotnet build` | Entire solution passed; zero warnings/errors |
| Complete `dotnet test` command below | 65 Application + 48 Infrastructure = **113 passed**, zero failures/skips; API scaffold has no tests before M4 |
| `npm run build` with bundled Node v24.19.0 | Passed; elevated retry after sandbox worker failure |
| `npm test -- --watch=false` with bundled Node v24.19.0 | **2 passed** in one file |
| `dotnet list package --include-transitive --no-restore` | Reviewed all projects; no EF, Dapper, MediatR, or unnecessary auth stack |
| Source review and forbidden-feature search | No refresh-token implementation, HTTP auth wiring, password/hash logging, or forbidden layer dependencies |
| `git diff --check` | Passed; only Git line-ending conversion notices |
| `git diff` and `git status --short` | Reviewed; changes limited to M3; no commit |

Final complete-suite command on this machine:

```powershell
dotnet test --no-restore --verbosity minimal --diag TestResults/m3-final.log -- RunConfiguration.DotNetHostPath=<historical-launcher-path>
```

Plain test invocations aborted before executing assertions because this machine redirects a requested loopback listener to its LAN address. The ignored launcher corrects the testhost endpoint argument only; standard VSTest/xUnit discovery and execution still run the entire suite. The workaround and failed attempts are documented in GENAI.md; no passing result is attributed to an aborted run.

## Security Review Findings

Reviewed persistence and real hashes, no password/hash logs, supported adaptive hashing, externally supplied signing secrets, expiration/signatures/claims, generic credential failures, one username comparison in application/storage, direct SQL uniqueness, insert-race translation, architecture dependencies, and scope. Found and corrected password exposure in the generated input record's diagnostic string; token diagnostics were redacted too. No remaining defect was found within M3 scope.

Generic login errors are identical at the application boundary; this does not promise equal execution timing for missing users. Token middleware/HTTP behavior is unimplemented and untested by design. Hasher results requesting rehash are accepted without automatically rewriting stored hashes. These limitations are documented in DEC-013.

## Assumptions and Deviations

Assumptions: username/password limits are measured in UTF-16 characters; ordinal case comparison is the identity policy, while accent/canonical normalization is not applied; signing keys are generated randomly and supplied externally; usable demo startup composition remains M4.

No deviations from project requirements, user story, or requested milestone scope. M3 intentionally supersedes DEC-011's temporary case-sensitive username decision as explicitly authorized. The test runner workaround is a validation environment deviation and remains outside tracked application source.

## Human Review Items

- Review DEC-012's collation and legacy-collision behavior: external SQL tools need collation registration; conflicting legacy accounts require deliberate resolution before initialization can succeed.
- Review DEC-013's hashing work factor, validation bounds, and externally configured JWT key/lifetime before composing M4 startup.
- Review the documented local test launcher or investigate this machine's loopback redirection; ordinary dotnet test should work on a normal loopback configuration.
- Supply local signing configuration in M4 and then bind options, seed demo data, configure JWT validation, and add HTTP integration tests under that milestone's prompt.

M3 is complete. No controllers/endpoints, Authorize attributes, JWT bearer middleware, HTTP error mapping, HttpContext identity extraction, Swagger authentication, CORS, Angular auth/task features, or refresh tokens were added. Work stops here without committing.
