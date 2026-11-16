# Codexo — Learning Log

## Module Status

| Module | Name | Status | Branch | Commits | Date |
|--------|------|--------|--------|---------|------|
| M0 | Scaffolding | IMPLEMENTED | `main` | `896762b..8d9b0c1` | 2026-11-15 |
| M1 | Domain + Hint Logic | IMPLEMENTED | `main` | `e8ffb00..9b7fc2b` | 2026-11-15 |
| M2 | Database | IMPLEMENTED | `main` | `719c47e..a073149` | 2026-11-15 |
| M2-CLEANUP | Database Refinements | IMPLEMENTED | `main` | `2831a96..79bd1f6` | 2026-11-16 |
| M3 | Authentication | IMPLEMENTED | `main` | `306ef41..1cb12cb` | 2026-11-16 |
| M4 | Problems + Sessions | IMPLEMENTED | `main` | `bae5309..711fe8d` | 2026-11-16 |
| M5 | AI Layer | IMPLEMENTED | `feature/m5-ai-layer` | `1f9f09f..[head]` | 2026-11-16 |
| M6 | Hint Orchestration | NOT IMPLEMENTED | — | — | — |
| M7 | Feedback + Preferences | NOT IMPLEMENTED | — | — | — |
| M8 | Hardening | NOT IMPLEMENTED | — | — | — |
| M9 | Extension Scaffold | NOT IMPLEMENTED | — | — | — |
| M10 | Content Script | NOT IMPLEMENTED | — | — | — |
| M11 | Side Panel UI | NOT IMPLEMENTED | — | — | — |
| M12 | Integration | NOT IMPLEMENTED | — | — | — |
| M13 | Release | NOT IMPLEMENTED | — | — | — |

---

*Learning entries will be appended below as modules are completed.*


## M0: Scaffolding

### What We Built
We set up the complete foundational structure for Codexo across .NET 10 and Chrome Extension targets. The solution contains three backend projects (`Codexo.Api`, `Codexo.Infrastructure`, `Codexo.Core`) and two test projects enforcing strict dependency inversion (`Api -> Infrastructure -> Core`). The extension shell separates the Angular 21 side-panel application from the MV3 background service worker and content script bundled via esbuild. Foundational documentation, security baseline, and GitHub Actions CI pipelines were established with zero business logic.

### Concepts
- **Clean Architecture Dependency Rule:** Source code dependencies point inward toward business concepts. `Codexo.Core` has no dependencies on database, Web API, or AI libraries. File: [src/Codexo.Core/Codexo.Core.csproj](file:///c:/Ariram/CodeXO/src/Codexo.Core/Codexo.Core.csproj).
- **Chrome Extension Manifest V3 (MV3):** Google's extension platform requiring non-persistent background service workers, dedicated Side Panel API, and banning remotely hosted/inline code. File: [extension/public/manifest.json](file:///c:/Ariram/CodeXO/extension/public/manifest.json).
- **Dual-Bundler Build Architecture:** Angular CLI compiles SPA components into static HTML/JS/CSS, while `esbuild` compiles the service worker and content script into isolated scripts without polluting browser scopes. File: [extension/scripts/build-extension.mjs](file:///c:/Ariram/CodeXO/extension/scripts/build-extension.mjs).

### Runtime Flow
```mermaid
graph TD
    A["Chrome Browser Tab (LeetCode)"] -->|"Matches URL"| B["content-script.js (IIFE)"]
    C["Side Panel Action Click"] -->|"chrome.sidePanel.open()"| D["service-worker.js (ESM)"]
    D -->|"Loads HTML/JS"| E["Angular Sidepanel (Standalone SPA)"]
    E -->|"Future HTTP API Calls"| F["Codexo.Api (ASP.NET Core)"]
    F --> G["Codexo.Infrastructure"]
    G --> H["Codexo.Core Domain"]
```

1. User opens a LeetCode problem page; Chrome injects `content-script.js`.
2. User triggers the extension icon; `service-worker.js` listens to `action.onClicked` and invokes `chrome.sidePanel.open()`.
3. Side panel loads `sidepanel/browser/index.html` hosting the standalone Angular application.
4. Future modules route requests to `Codexo.Api`, propagating downward to Core.

### Design Decisions & Rejected Alternatives
- **Decision:** Bundled content script and background worker using `esbuild` rather than adding multiple projects to `angular.json`.
  *Rejected alternative:* Multi-project Angular workspace for extension scripts. MV3 forbids DOM access in service workers and requires specific module formats that Angular's application builder complicates.
- **Decision:** Used .NET 10 solution (`.slnx`) with project references enforcing `Api -> Infrastructure -> Core`.
  *Rejected alternative:* Direct reference from Core to external packages.

### Interview Questions & Model Answers
- **Q: Why separate the content script and service worker build from Angular's application build in an MV3 extension?**
  *A:* MV3 content scripts run directly in the webpage context (IIFE format) while background service workers run in an isolated worker context without DOM access. Angular's CLI targets browser SPAs; bundling background scripts separately via esbuild gives precise control over formats, sourcemaps, and avoids loading heavy framework runtimes where unnecessary.
- **Q: Why does `Codexo.Core` have zero package references to EF Core or ASP.NET Core?**
  *A:* Clean architecture ensures core domain rules and algorithms are independent of UI frameworks, persistence mechanisms, or external services. This allows pure, fast unit testing without mocking HTTP contexts or databases.
- **Q: Why is `outputHashing: "none"` set for the Angular extension build?**
  *A:* Chrome extensions require static paths defined in `manifest.json`. Cache-busted hash names break references like `sidepanel/browser/index.html`.

### Run & Test Commands
```bash
# Build backend solution
dotnet build Codexo.slnx

# Run all backend unit & integration tests
dotnet test Codexo.slnx --verbosity normal

# Install and build extension
cd extension
npm run build
```

### Common Pitfalls
- **Manifest path mismatches:** Angular 21 defaults output to `dist/codexo/sidepanel/browser/index.html`. Ensure `manifest.json` points to the exact path generated.
- **Leaking references into Core:** Accompanying NuGet packages like `Microsoft.AspNetCore.Http` into Core violates architectural boundaries.


## M1: Domain + Hint Logic

### What We Built
We implemented the domain model and hint-safety engine in `Codexo.Core` with zero external dependencies. The domain entities model DSA problems, user sessions, progressive hints, attempts, and feedback. `HintLadder` enforces server-authoritative 5-level progression (L1 to L5) and prevents duplicate level advancements. `AttemptGatingValidator` validates that unlocking L2+ requires a meaningful written attempt, rejecting empty submissions, keysmash gibberish, trivial one-liners, and full-solution solicitations. `SolutionGuard` and `AiResponseValidator` structurally inspect AI outputs for markdown code blocks, compilable statements, full solution dumps, and level-specific spoilers (banning algorithms/data structures at L1 and algorithm/pattern names at L2).

### Concepts
- **Server-Authoritative Progression:** The server alone computes and advances hint levels; client-provided level values are ignored to prevent tampering or skipping ahead. File: [src/Codexo.Core/Hints/HintLadder.cs](file:///c:/Ariram/CodeXO/src/Codexo.Core/Hints/HintLadder.cs).
- **Structural Solution Guarding:** Rather than relying merely on naive keyword searches, `SolutionGuard` detects programming syntax patterns (function declarations, semicolon-terminated assignments, loops) and code fences to guarantee that no compilable code is leaked. File: [src/Codexo.Core/Hints/SolutionGuard.cs](file:///c:/Ariram/CodeXO/src/Codexo.Core/Hints/SolutionGuard.cs).
- **Attempt Gating:** A pedagogical safeguard that blocks hints past L1 until the user submits a verifiable written reflection (minimum 15 characters, 3 words, non-trivial, non-gibberish) describing what they tried or understood. File: [src/Codexo.Core/Hints/AttemptGatingValidator.cs](file:///c:/Ariram/CodeXO/src/Codexo.Core/Hints/AttemptGatingValidator.cs).

### Runtime Flow
```mermaid
graph TD
    A["User Request: Next Hint"] --> B["HintLadder: Determine Target Level"]
    B --> C{"Requires Attempt? (L2-L5)"}
    C -->|"Yes"| D["AttemptGatingValidator: Validate Attempt"]
    D -->|"Invalid/Empty/Junk"| E["Reject with RejectionReason"]
    D -->|"Valid"| F["Advance Level & Request Hint"]
    C -->|"No (L1)"| F
    F --> G["AI Response Generated"]
    G --> H["SolutionGuard: Structural Inspection"]
    H -->|"Violations Found"| I["Regenerate (1x) or Fallback Hint"]
    H -->|"Clean"| J["Return Safe Progressive Hint"]
```

1. User requests a hint for their active session; `HintLadder` calculates the next level.
2. If advancing past L1, `AttemptGatingValidator` checks the user's attempt for length, word count, gibberish, and solution begging.
3. Once validated, the AI generates candidate hint content.
4. `SolutionGuard` inspects the text for code blocks, compilable statements, and spoilers.
5. If safe, the hint is persisted and returned; otherwise a constrained retry or static fallback is returned.

### Design Decisions & Rejected Alternatives
- **Decision:** Structural parsing using regex and token heuristics for code detection rather than full language AST parsers.
  *Rejected alternative:* Incorporating Roslyn or Tree-sitter for multiple languages. Regex/structural patterns cover multi-language code snippets with zero external dependencies and sub-millisecond execution.
- **Decision:** Consonant-cluster and n-gram analysis for keysmash detection rather than keyboard-row letter matching.
  *Rejected alternative:* Row regex (`^[qwertyuiop]+$`). This naive check falsely rejected legitimate English words like "error" and "require".

### Interview Questions & Model Answers
- **Q: Why must hint progression be server-authoritative rather than client-driven?**
  *A:* Trusting the client allows users to inspect network calls or manipulate browser state to skip levels, bypass attempt requirements, or receive final solutions immediately. Server authority guarantees progressive learning pedagogy and idempotency against parallel duplicate clicks.
- **Q: How does `SolutionGuard` prevent false positives when users write algorithmic explanations?**
  *A:* It separates structural code indicators (code fences, return types, semicolons, assignment operators) from natural language explanations, allowing conceptual discussion while strictly blocking compilable implementation syntax.

### Run & Test Commands
```bash
# Run Core unit tests
dotnet test tests/Codexo.Core.Tests/Codexo.Core.Tests.csproj --verbosity normal
```

### Common Pitfalls
- **Overly aggressive keysmash detection:** Testing for repetitive keyboard rows flags normal words. Use consonant clustering and letter distribution metrics instead.
- **Spoiler leakage in L1/L2:** High-level algorithmic names (e.g., "Two Pointers", "Hash Map") spoil the core problem-solving deduction step and must be quarantined until L3.


## M2: Database

### What We Built
We designed and implemented the relational persistence layer in `Codexo.Infrastructure` using Entity Framework Core 10 with SQL Server LocalDB (`MSSQLLocalDB`). We configured six domain entities using the Fluent API across dedicated configuration classes, enforcing primary keys, column constraints, composite indexes, and cascading/restricted delete behaviors. We established the initial EF Core migration (`InitialCreate`) and an idempotent `DbInitializer` seeding benchmark problems (*Two Sum*, *Valid Anagram*, *Merge Two Sorted Lists*) and development preferences. Five integration tests were implemented using `WebApplicationFactory` to verify migrations, unique constraints, and cascades.

### Concepts
- **Fluent API Entity Configuration:** Decoupling entity modeling from domain entities into separate `IEntityTypeConfiguration<T>` classes keeps domain models pure and persistence concerns encapsulated. File: [src/Codexo.Infrastructure/Data/EntityConfigurations/ProblemConfiguration.cs](file:///c:/Ariram/CodeXO/src/Codexo.Infrastructure/Data/EntityConfigurations/ProblemConfiguration.cs).
- **Composite Unique Constraints:** Enforcing uniqueness across `(Platform, PlatformProblemId)` prevents duplicate problems from being inserted into the database. File: [src/Codexo.Infrastructure/Data/EntityConfigurations/ProblemConfiguration.cs](file:///c:/Ariram/CodeXO/src/Codexo.Infrastructure/Data/EntityConfigurations/ProblemConfiguration.cs).
- **Idempotent Database Initialization:** Seeding checks if records already exist prior to inserting, ensuring that server restarts do not cause unique index collision errors. File: [src/Codexo.Infrastructure/Data/DbInitializer.cs](file:///c:/Ariram/CodeXO/src/Codexo.Infrastructure/Data/DbInitializer.cs).

### Runtime Flow
```mermaid
graph TD
    A["API Startup / Integration Test"] --> B["DbInitializer.SeedAsync()"]
    B --> C["context.Database.GetPendingMigrationsAsync()"]
    C -->|"Pending Found"| D["context.Database.MigrateAsync()"]
    C -->|"Up-to-date"| E["Check context.Problems.AnyAsync()"]
    D --> E
    E -->|"Empty"| F["Seed Problems & UserPreference"]
    E -->|"Exists"| G["Skip Seeding (Idempotent)"]
```

1. Application or test host boots up.
2. In Development, `DbInitializer.SeedAsync` checks for pending EF Core migrations and applies them automatically.
3. The initializer checks if any problems exist in the database.
4. If empty, it persists seed records; if records exist, it exits safely.

### Design Decisions & Rejected Alternatives
- **Decision:** Used SQL Server LocalDB with explicit Fluent API configuration classes rather than data annotations on domain entities.
  *Rejected alternative:* Using Data Annotations (`[Key]`, `[MaxLength]`) on Core entities. In Clean Architecture, domain entities should remain framework-agnostic.
- **Decision:** Pinned NuGet packages to `10.0.*` to ensure compatibility with .NET 10.
  *Rejected alternative:* Using unpinned or floating versions, which pulled in EF Core 11 daily builds incompatible with the .NET 10 runtime.

### Interview Questions & Model Answers
- **Q: Why use Fluent API over Data Annotations in Clean Architecture?**
  *A:* Data Annotations place persistence and database metadata directly on domain classes, coupling domain entities to database concepts and schema details. The Fluent API separates entity definitions from relational schema mapping, keeping the Core domain pure.
- **Q: How do you prevent migration race conditions or crashes in multi-instance or test environments?**
  *A:* By checking `GetPendingMigrationsAsync()` and ensuring seeding operations are strictly idempotent with `AnyAsync()` checks before writes.
- **Q: Why is composite indexing necessary on `(Platform, PlatformProblemId)`?**
  *A:* Different platforms (e.g. LeetCode and HackerRank) might use the same problem IDs (e.g. "1"). A composite unique index guarantees uniqueness per platform while speeding up lookups by platform and platform ID.

### Run & Test Commands
```bash
# Add EF Core migration
dotnet ef migrations add <MigrationName> --project src/Codexo.Infrastructure --startup-project src/Codexo.Api

# Update LocalDB database
dotnet ef database update --project src/Codexo.Infrastructure --startup-project src/Codexo.Api

# Run integration tests against LocalDB
dotnet test tests/Codexo.Api.IntegrationTests/Codexo.Api.IntegrationTests.csproj
```

### Common Pitfalls
- **Package Version Drift:** Adding EF Core packages without specifying version constraints can pull in next-major daily builds (e.g. 11.0.0 on a net10.0 target). Pin to matching LTS versions (`10.0.*`).
- **Missing Foreign Key Cascade Rules:** Neglecting cascade deletes causes orphaned records in child tables (e.g. hints or attempts when a session is cleared).


## M2-CLEANUP: Database Refinements

### What We Built
We refined the database architecture to safeguard user solving history by altering the `Problem -> ProblemSession` foreign key delete behavior from `Cascade` to `Restrict`. We generated EF Core migration `RestrictProblemDeleteBehavior` and applied it to SQL Server LocalDB (`MSSQLLocalDB`). We updated `DbInitializer` to enforce development-only execution and explicitly labeled `seed-user-1` as a dev placeholder prior to M3 Identity integration. We documented the complete Mermaid entity relationship diagram in documentation and established `.githooks/pre-push` to enforce build and test gates before pushing.

### Concepts
- **Referential Integrity Restrict Action:** Changing foreign key deletion from `Cascade` to `Restrict` (`ON DELETE NO ACTION` in SQL Server) prevents accidental deletion of parent problem records whenever user study sessions reference them. File: [src/Codexo.Infrastructure/Data/EntityConfigurations/ProblemSessionConfiguration.cs](file:///c:/Ariram/CodeXO/src/Codexo.Infrastructure/Data/EntityConfigurations/ProblemSessionConfiguration.cs).
- **Environment-Gated Data Seeding:** Restricting database seeding and automatic migrations strictly to `Development` ensures production environments rely exclusively on formal CI/CD database migration pipelines. File: [src/Codexo.Infrastructure/Data/DbInitializer.cs](file:///c:/Ariram/CodeXO/src/Codexo.Infrastructure/Data/DbInitializer.cs).
- **Pre-Push Git Hook:** A client-side Git hook that compiles the solution and executes all test suites, terminating the push command with a non-zero exit code if any test fails. File: [.githooks/pre-push](file:///c:/Ariram/CodeXO/.githooks/pre-push).

### Runtime Flow
```mermaid
graph TD
    A["Problem Delete Attempted"] --> B["EF Core Change Tracker"]
    B --> C["SQL Server Foreign Key Evaluation"]
    C --> D{"ProblemSessions exist for Problem?"}
    D -->|"Yes"| E["SQL Server Rejects with FK Violation"]
    E --> F["EF Core Throws DbUpdateException"]
    D -->|"No"| G["Problem Deleted Successfully"]
```

1. Application attempts to delete a `Problem` entity.
2. EF Core issues a `DELETE FROM Problems WHERE Id = @id` statement to SQL Server.
3. SQL Server checks constraint `FK_ProblemSessions_Problems_ProblemId`.
4. If any `ProblemSession` records reference the target problem, the server aborts the transaction with an FK violation.
5. EF Core wraps the error and surfaces a `DbUpdateException`, preventing data loss.

### Design Decisions & Rejected Alternatives
- **Decision:** Set `Problem -> ProblemSession` to `DeleteBehavior.Restrict` while retaining `Cascade` on `ProblemSession -> Hints` and `ProblemSession -> UserAttempts`.
  *Rejected alternative:* Cascade delete from Problem all the way down. If an administrator deletes or re-imports a problem, cascading would wipe out every user's personal attempt history and hint ladder state.
- **Decision:** Created `.githooks/pre-push` running `dotnet build` and `dotnet test`.
  *Rejected alternative:* Relying solely on remote CI. Pre-push hooks catch regressions locally before pushing broken commits to remote branches.

### Interview Questions & Model Answers
- **Q: Why should parent entities like Problems use `Restrict` instead of `Cascade` when referenced by user activity logs?**
  *A:* Cascading deletions on master/catalog data can result in catastrophic unintentional data destruction. If a problem record is removed, cascading would erase all associated user learning logs, attempts, and progress. Restrict forces administrators to either archive problems or reassign sessions.
- **Q: How does EF Core handle `DeleteBehavior.Restrict` differently when dependent entities are loaded in memory vs only in the database?**
  *A:* If dependents are loaded in the `DbContext` change tracker, EF Core attempts to nullify the foreign key client-side (throwing `InvalidOperationException` if required). If dependents exist only in the database, EF Core sends the `DELETE` command and SQL Server throws a `DbUpdateException`.
- **Q: Why should database seeding routines check `app.Environment.IsDevelopment()`?**
  *A:* Automatic seeding in production can overwrite live records, introduce dummy test users into security audits, or trigger race conditions in multi-instance horizontally scaled deployments.

### Run & Test Commands
```bash
# Enable pre-push hook locally
git config core.hooksPath .githooks

# Run all tests across solution
dotnet test Codexo.slnx --verbosity normal
```

### Common Pitfalls
- **Untracked Test Collisions:** Integration tests that insert static IDs can fail on repeated runs if previous test runs failed before cleanup. Always use unique identifiers (e.g. `Guid.NewGuid()`) for test problem records.
- **Severed Required Relationships in Memory:** Attempting to delete a parent with tracked children under `Restrict` trips client-side conceptual null checks before the SQL constraint can even be reached.


## M3: Authentication

### What We Built
We implemented complete token-based authentication and user management using **ASP.NET Core Identity** and **JSON Web Tokens (JWT)**. `AppDbContext` was upgraded to inherit from `IdentityDbContext<AppUser>`, introducing ASP.NET Core Identity tables (`AspNetUsers`, `AspNetRoles`, `AspNetUserClaims`, etc.) and establishing foreign key relationships from `ProblemSessions`, `UserPreferences`, and `HintFeedbacks` to `AspNetUsers(Id)`, eliminating the `seed-user-1` placeholder. We created `TokenService` implementing `ITokenService` to issue HMAC-SHA256 signed JWTs containing `sub`, `email`, and `jti` claims. We implemented `AuthController` endpoints (`/api/auth/register`, `/api/auth/login`, and protected `/api/auth/me`) guarded by FluentValidation and account enumeration defense. Nine integration tests verify registration, password complexity validation, login, token tampering detection, and claims authorization.

### Concepts
- **ASP.NET Core Identity Integration with Clean Architecture:** `AppUser` extends `IdentityUser` inside `Codexo.Infrastructure` while `Codexo.Core` maintains agnostic domain models storing `UserId` as a GUID string. EF Core Fluent configurations map foreign keys (`FK_ProblemSessions_AspNetUsers_UserId`) cleanly without polluting the Core domain with Identity dependencies. File: [src/Codexo.Infrastructure/Identity/AppUser.cs](file:///c:/Ariram/CodeXO/src/Codexo.Infrastructure/Identity/AppUser.cs).
- **Cryptographic JWT Authentication:** Tokens are signed using symmetric HMAC-SHA256 credentials with keys retrieved via .NET User Secrets or secure configuration. Token validation parameters enforce issuer, audience, signature, and zero clock skew. File: [src/Codexo.Infrastructure/Auth/TokenService.cs](file:///c:/Ariram/CodeXO/src/Codexo.Infrastructure/Auth/TokenService.cs).
- **Account Enumeration Defense:** Registration and login endpoints provide uniform error responses (`"Invalid email or password."`) whether an email exists or does not exist, eliminating timing and content side-channels that attackers exploit to harvest registered user emails. File: [src/Codexo.Api/Controllers/AuthController.cs](file:///c:/Ariram/CodeXO/src/Codexo.Api/Controllers/AuthController.cs).
- **SQL Server Multiple Cascade Paths Defense:** SQL Server prohibits multiple cascade delete paths leading to the same child table. Because deleting an `AppUser` cascades through `ProblemSessions -> Hints -> HintFeedbacks`, the direct foreign key from `AspNetUsers` to `HintFeedbacks` was configured with `DeleteBehavior.Restrict` (`ON DELETE NO ACTION`), while `ProblemSessions` and `UserPreferences` use `Cascade`. File: [src/Codexo.Infrastructure/Data/EntityConfigurations/HintFeedbackConfiguration.cs](file:///c:/Ariram/CodeXO/src/Codexo.Infrastructure/Data/EntityConfigurations/HintFeedbackConfiguration.cs).
- **Startup Secret Presence Check:** In accordance with Codexo security rules, the application verifies that the JWT signing key is configured and at least 32 bytes at startup, reporting strictly `[Startup Check] JWT signing key configured: yes/no` without exposing secret values to logs or consoles. File: [src/Codexo.Api/Program.cs](file:///c:/Ariram/CodeXO/src/Codexo.Api/Program.cs).

### Runtime Flow
```mermaid
sequenceDiagram
    autonumber
    actor Client as Chrome Extension / Web Client
    participant Auth as AuthController
    participant Identity as UserManager<AppUser>
    participant Token as TokenService
    participant Protected as Protected Endpoint (/api/auth/me)

    Note over Client, Auth: User Login Flow
    Client->>Auth: POST /api/auth/login { email, password }
    Auth->>Identity: FindByEmailAsync(email)
    Auth->>Identity: CheckPasswordAsync(user, password)
    alt Invalid Credentials or Unknown Email
        Auth-->>Client: 401 Unauthorized ("Invalid email or password.")
    else Valid Credentials
        Auth->>Token: GenerateToken(user.Id, user.Email)
        Token-->>Auth: Signed JWT Token
        Auth-->>Client: 200 OK { token, expiresAtUtc, userId, email }
    end

    Note over Client, Protected: Authenticated Request Flow
    Client->>Protected: GET /api/auth/me (Authorization: Bearer <token>)
    Note over Protected: JwtBearerMiddleware validates signature, issuer, audience, expiry
    alt Missing, Expired, or Tampered Token
        Protected-->>Client: 401 Unauthorized
    else Valid Token
        Protected-->>Client: 200 OK { userId, email }
    end
```

### Design Decisions & Rejected Alternatives
- **Decision:** Retained string `UserId` on `Codexo.Core` domain entities and mapped foreign keys in `Codexo.Infrastructure` via Fluent API without navigation properties on Core entities.
  *Rejected alternative:* Having `ProblemSession` reference `AppUser` directly. This would violate the Clean Architecture dependency rule by forcing `Codexo.Core` to depend on `Microsoft.AspNetCore.Identity.EntityFrameworkCore`.
- **Decision:** Configured `DeleteBehavior.Restrict` on `HintFeedback -> AppUser` and `Cascade` on `ProblemSession -> AppUser`.
  *Rejected alternative:* Cascade on all user foreign keys. SQL Server rejected the schema migration with error 1785 (cycles or multiple cascade paths) because `HintFeedbacks` would receive cascade delete signals from both `Hints` and `AspNetUsers`.
- **Decision:** Short access token lifetime (60 minutes) with refresh tokens specified as design-only in M3.
  *Rejected alternative:* Building complex refresh token rotation endpoints before extension client storage architecture is implemented.

### Interview Questions & Model Answers
- **Q: How did you implement Clean Architecture when integrating ASP.NET Core Identity with your domain entities?**
  *A:* ASP.NET Core Identity is an infrastructure and framework concern. In `Codexo.Core`, our domain entities (`ProblemSession`, `HintFeedback`, `UserPreference`) define `UserId` as a pure primitive `string` with no dependencies on Identity. In `Codexo.Infrastructure`, `AppDbContext` inherits from `IdentityDbContext<AppUser>`, and entity configurations establish relational foreign keys to `AspNetUsers(Id)` via Fluent API shadow foreign keys (`builder.HasOne<AppUser>().WithMany().HasForeignKey(s => s.UserId)`). This keeps our Core domain completely agnostic of authentication frameworks.
- **Q: Why did SQL Server reject the migration when configuring cascade delete from AspNetUsers to HintFeedbacks, and how did you resolve it?**
  *A:* SQL Server prohibits multiple cascade delete paths to a single table to prevent referential ambiguity and delete cycles. In our schema, deleting an `AppUser` cascades through `ProblemSessions -> Hints -> HintFeedbacks`. Adding a direct cascade foreign key from `AspNetUsers` to `HintFeedbacks` created two simultaneous cascade paths to `HintFeedbacks`. We resolved this by setting the direct relationship `HintFeedbacks.UserId -> AspNetUsers.Id` to `DeleteBehavior.Restrict` (`ON DELETE NO ACTION`), allowing the cascade to proceed exclusively through the session/hint hierarchy.
- **Q: What is account enumeration, and how does your API defend against it?**
  *A:* Account enumeration occurs when an API provides different responses (e.g. "Email does not exist" vs "Incorrect password") or observable execution timing differences that allow an attacker to determine whether a given email address has an active account. In Codexo, both registration and login endpoints return uniform, non-distinguishing error messages and HTTP status codes (`401 Unauthorized: "Invalid email or password."`) regardless of whether the email was registered or the password was incorrect.

### Run & Test Commands
```bash
# Set local dev JWT key securely in User Secrets
dotnet user-secrets set "Jwt:Key" "<strong-32-byte-secret>" --project src/Codexo.Api/Codexo.Api.csproj

# Run all unit and integration tests (85 total)
dotnet test Codexo.slnx --verbosity normal
```

### Common Pitfalls
- **SQL Server Error 1785 (Multiple Cascade Paths):** SQL Server does not permit multiple cascade delete paths leading to the same table. Always trace all foreign key paths and apply `Restrict` / `NoAction` to redundant paths.
- **Leaking Secrets in Console/Logs:** Never print secret keys or configuration values during startup checks. Check key presence and length, and output only boolean indicators (`yes/no`).


## M4: Problems + Sessions

### What We Built
We implemented problem context intake, catalog deduplication, and user study session lifecycle management in `Codexo.Infrastructure` and `Codexo.Api`. We established DTOs and contracts in `Codexo.Core` (`ProblemIntakeRequest`, `ProblemDto`, `ProblemSessionResponse`, and `ISessionService`). In `SessionService`, incoming problem statements from DSA platforms (such as LeetCode) are validated against strict length limits (with descriptions bounded up to 20,000 characters) and untrusted input constraints via FluentValidation. Problems are idempotently get-or-created by composite key `(Platform, PlatformProblemId)`, updating catalog text when details change. The service seamlessly retrieves or initializes the user's active session starting at `L1_Understand` and handles browser tab context switching without data corruption. Above all, **strict per-user data isolation** is enforced across all session endpoints, verified by integration tests proving User B receives `404 Not Found` when attempting to access User A's session.

### Concepts
- **Untrusted Input Defense & Content Bounding:** Content extracted from third-party web pages must be treated as untrusted user input. FluentValidation enforces strict structural rules (valid platform enum, URL format, title $\le$ 256 chars, description $\le$ 20,000 chars) to prevent payload-based denial of service or prompt-injection attacks. File: [src/Codexo.Api/Validators/ProblemIntakeRequestValidator.cs](file:///c:/Ariram/CodeXO/src/Codexo.Api/Validators/ProblemIntakeRequestValidator.cs).
- **Catalog Upsert & Problem Deduplication:** DSA problems belong to a shared catalog identified by composite unique index `(Platform, PlatformProblemId)`. Multiple users studying the same problem share the single underlying `Problem` record while maintaining isolated `ProblemSession` records. File: [src/Codexo.Infrastructure/Services/SessionService.cs](file:///c:/Ariram/CodeXO/src/Codexo.Infrastructure/Services/SessionService.cs).
- **Per-User Isolation via Claims Principal:** User identity is never accepted from request query strings or bodies. The server extracts the authenticated `UserId` directly from the validated JWT claims (`ClaimTypes.NameIdentifier`). Every database query enforces `WHERE s.UserId == userId`. File: [src/Codexo.Api/Controllers/SessionsController.cs](file:///c:/Ariram/CodeXO/src/Codexo.Api/Controllers/SessionsController.cs).
- **Enumeration Defense via 404 Responses:** When a user requests a session ID belonging to another user, the API returns `404 Not Found` rather than `403 Forbidden`. This conceals the existence of other users' session GUIDs from attackers. File: [src/Codexo.Api/Controllers/SessionsController.cs](file:///c:/Ariram/CodeXO/src/Codexo.Api/Controllers/SessionsController.cs).

### Runtime Flow
```mermaid
sequenceDiagram
    autonumber
    actor Extension as Chrome Extension
    participant API as SessionsController
    participant Service as SessionService
    participant DB as AppDbContext (SQL Server)

    Extension->>API: POST /api/sessions/context (ProblemIntakeRequest) [Bearer Token]
    Note over API: Extract authenticated UserId from JWT claims
    API->>API: Validate request via ProblemIntakeRequestValidator
    API->>Service: GetOrCreateSessionAsync(userId, request)
    Service->>DB: Query Problems by (Platform, PlatformProblemId)
    alt Problem Not in Catalog
        Service->>DB: Insert new Problem
    else Problem Exists & Details Changed
        Service->>DB: Update Problem (Title, Description, Url)
    end
    Service->>DB: Query ProblemSessions for (UserId, ProblemId, Status == Active)
    alt Active Session Exists
        Service->>DB: Update LastActivityAt
    else No Active Session
        Service->>DB: Insert new ProblemSession (Level = L1_Understand, Status = Active)
    end
    Service-->>API: Return ProblemSessionResponse
    API-->>Extension: 200 OK (Session details)
```

### Design Decisions & Rejected Alternatives
- **Decision:** Returned `404 Not Found` rather than `403 Forbidden` when accessing a cross-user session.
  *Rejected alternative:* Returning `403 Forbidden`. While semantically descriptive, `403` informs an attacker that a resource ID exists in the system, opening an ID enumeration vulnerability.
- **Decision:** Implemented `ISessionService` directly using `AppDbContext` without introducing a Generic Repository layer.
  *Rejected alternative:* Creating `IProblemRepository` and `ISessionRepository`. Codexo Build Rules forbid adding an extra Repository abstraction over EF Core, which already acts as an in-memory repository and unit of work.
- **Decision:** Bound problem description lengths to 20,000 characters.
  *Rejected alternative:* Unbounded string inputs or tiny 1,000 character limits. Real DSA interview questions (e.g. text-heavy geometry or graph problems with long constraints) frequently exceed 2,000 characters, while 20,000 characters provides ample space while protecting database buffers.

### Interview Questions & Model Answers
- **Q: How does Codexo guarantee multi-tenant/per-user data isolation on study sessions?**
  *A:* Multi-tenant data isolation is enforced at three levels: (1) Authentication: Endpoints require a valid JWT, and the user identity is extracted strictly from the cryptographically verified `NameIdentifier` claim, never from client-supplied request parameters. (2) Query filtering: Every database retrieval executes with an explicit `WHERE s.UserId == authenticatedUserId` clause. (3) Security responses: Requests attempting to read or manipulate records belonging to another user return `404 Not Found`, ensuring unauthorized users cannot even verify the existence of another user's session IDs.
- **Q: How did you implement problem deduplication when thousands of users might study the same LeetCode problem?**
  *A:* In our relational schema, `Problems` is a shared catalog table indexed with a unique composite constraint on `(Platform, PlatformProblemId)`. When a user requests hints, `SessionService` performs an idempotent upsert: if the problem exists, its record is reused and refreshed if text changed; if not, a new catalog entry is created. Each user's study session is then linked to that shared `ProblemId` via a separate `ProblemSession` record.
- **Q: Why should webpage content extracted by a Chrome extension content script be treated as untrusted input?**
  *A:* Web content scripts run in the DOM of arbitrary external websites. Malicious or compromised pages could inject arbitrary HTML, oversized payloads to induce memory exhaustion, or crafted text designed to manipulate downstream LLMs (prompt injection). Validating lengths, sanitizing formats, and enforcing strict input boundaries at the API gateway protects the database and AI pipeline.

### Run & Test Commands
```bash
# Run all unit and integration tests (90 total)
dotnet test Codexo.slnx --verbosity normal
```

### Common Pitfalls
- **Accidental Cross-User Data Leaks:** Forgetting to filter by `UserId` when retrieving entities by GUID primary key (`context.ProblemSessions.FindAsync(id)`) allows any authenticated user to inspect another user's data if they obtain or guess the GUID. Always query with `FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId)`.
- **String Input Nullability & Whitespace:** DSA problem scrapers frequently return trailing whitespace or omitted difficulty fields. Always trim strings and provide fallback defaults (`"Unknown"`) before persistence.


## M5: AI Layer (Gemini Integration & Evaluation Harness)

### What We Built
We built the complete AI abstraction layer connecting Codexo to Google's Gemini API via an OpenAI-compatible endpoint, while preserving complete offline determinism for testing and CI. We authored `IAiClient`, structured DTOs (`AiPromptRequest`, `AiHintResponse`), domain exceptions (`AiQuotaExceededException`, `AiAuthenticationException`, `AiServiceUnavailableException`), and `HintPromptBuilder` that compiles embedded prompt templates (`Base.txt`, `L1.txt`..`L5.txt`) with delimiter-wrapped user data for prompt-injection defense. We built `TolerantJsonParser` for resilient schema extraction and `OpenAiCompatibleAiClient` targeting `gemini-4-hydrogen` with low reasoning effort, exponential backoff for transient 503 spikes, 429 quota handling, and sanitized token-only audit logging. A 10-problem benchmark evaluation harness evaluated 50 total hints against `SolutionGuard` (100% compliance), and an opt-in live smoke test verified live Gemini API connectivity.

### Concepts
- **Embedded Prompt Templates:** Storing prompt templates as embedded resources (`EmbeddedResource`) in `Codexo.Core.dll` ensures prompts are version-controlled, bundled with the binary, and accessible at runtime without fragile relative file paths. File: [src/Codexo.Core/Prompts/HintPromptBuilder.cs](file:///c:/Ariram/CodeXO/src/Codexo.Core/Prompts/HintPromptBuilder.cs).
- **Delimiter Wrapping & Prompt Injection Defense:** Wrapping untrusted user data (attempts, descriptions) within explicit `<USER_DATA>` and `<DELIMITER>` tags prevents page content or malicious inputs from overriding system instructions. File: [src/Codexo.Core/Prompts/HintPromptBuilder.cs](file:///c:/Ariram/CodeXO/src/Codexo.Core/Prompts/HintPromptBuilder.cs).
- **Tolerant JSON Parsing:** LLMs may wrap JSON responses in markdown code fences (````json ... ````) or include conversational preambles. `TolerantJsonParser` extracts the outermost JSON object and guarantees non-null collection properties. File: [src/Codexo.Infrastructure/Ai/TolerantJsonParser.cs](file:///c:/Ariram/CodeXO/src/Codexo.Infrastructure/Ai/TolerantJsonParser.cs).
- **Reasoning Effort Optimization:** Controlling model thinking depth via `reasoning_effort: "low"` slashes latency (from >30s timeouts down to ~4s) and minimizes expensive billed reasoning tokens. File: [src/Codexo.Infrastructure/Ai/OpenAiCompatibleAiClient.cs](file:///c:/Ariram/CodeXO/src/Codexo.Infrastructure/Ai/OpenAiCompatibleAiClient.cs).
- **Zero-Secret Diagnostics:** Diagnostic checks at startup check presence without logging keys (`API key configured: yes/no`). Audit logs record strictly token counts, never headers or prompts. File: [src/Codexo.Api/Program.cs](file:///c:/Ariram/CodeXO/src/Codexo.Api/Program.cs).

### Runtime Flow
```mermaid
sequenceDiagram
    autonumber
    participant Caller as Hint Service (M6)
    participant Client as OpenAiCompatibleAiClient
    participant Builder as HintPromptBuilder
    participant Gemini as Google Gemini API (gemini-4-hydrogen)
    participant Parser as TolerantJsonParser
    participant Guard as SolutionGuard

    Caller->>Client: GenerateHintAsync(AiPromptRequest)
    Client->>Builder: BuildPrompt(request)
    Builder->>Builder: Load Base.txt + Level.txt from Assembly
    Builder->>Builder: Wrap problem, attempt, & history in delimiters
    Builder-->>Client: (SystemPrompt, UserPrompt)
    Client->>Gemini: POST /chat/completions (model, reasoning_effort="low", response_format=json)
    alt Transient 503 High Demand
        Gemini-->>Client: HTTP 503 Service Unavailable
        Client->>Client: Exponential backoff & retry (up to 3 attempts)
    end
    Gemini-->>Client: 200 OK (Envelope with choices[0].message.content)
    Client->>Client: Log token counts (prompt, completion, total) - NO secrets
    Client->>Parser: Parse(content, targetLevel)
    Parser->>Parser: Strip fences, extract JSON object, normalize arrays
    Parser-->>Client: AiHintResponse
    Client-->>Caller: AiHintResponse
    Caller->>Guard: Inspect(targetLevel, response.Hint)
    Guard-->>Caller: GuardResult (IsValid, Details)
```

### Design Decisions & Rejected Alternatives
- **Decision:** Targeted `gemini-4-hydrogen` via Google's OpenAI-compatible endpoint with `reasoning_effort = "low"`.
  *Rejected alternative:* Creating a separate proprietary Google SDK client or using heavy thinking budgets. Google's OpenAI-compatible endpoint provides clean provider independence, and `gemini-4-hydrogen` delivered sub-5-second validated hints.
- **Decision:** Built `FakeAiClient` with deterministic static responses tailored to each level contract (L1–L5).
  *Rejected alternative:* Relying on mock HTTP handlers or requiring live API keys in CI. Offline tests must execute fast, deterministically, and with zero network dependency.
- **Decision:** Embedded prompt templates directly into the `Codexo.Core` assembly.
  *Rejected alternative:* Reading prompt files from disk at runtime with `File.ReadAllText`. Deploying to Docker, Azure, or different working directories frequently breaks relative file paths.

### Interview Questions & Model Answers
- **Q: How does Codexo protect against prompt injection when processing untrusted problem text from external DSA platforms?**
  *A:* Codexo uses structural delimiter wrapping and role separation. System instructions (loaded from embedded templates) specify strict behavioral boundaries and output schemas. All untrusted external inputs—including problem descriptions scraped from web pages, previous hint histories, and user attempt text—are isolated within labeled `<USER_DATA>` and `<DELIMITER>` tags in the user prompt, with explicit system instructions that content inside delimiters must never be interpreted as instructions.
- **Q: Why is tolerant JSON parsing essential when integrating LLMs into production microservices?**
  *A:* Even with structured outputs enabled (`response_format: { type: "json_object" }`), LLMs can occasionally enclose JSON in markdown code blocks (` ```json `), include leading/trailing whitespace, or omit optional collection arrays. A tolerant parser strips markdown artifacts, extracts the first valid JSON substring, deserializes the envelope, and defensively normalizes null collections to empty arrays, preventing deserialization crashes from disrupting the user experience.
- **Q: How did you optimize Gemini API latency and costs for real-time hint generation?**
  *A:* We configured `reasoning_effort: "low"` on `gemini-4-hydrogen`. Because reasoning tokens are billed as output tokens and slow down generation, setting reasoning effort to low reduced response latency from >30 seconds down to ~4 seconds while remaining within token budget constraints (`max_tokens: 800`).

### Run & Test Commands
```bash
# Run all unit and integration tests (109 total)
dotnet test Codexo.slnx --verbosity normal

# Run AI evaluation harness benchmark (10 DSA problems across 5 levels)
dotnet test tests/Codexo.Api.IntegrationTests/Codexo.Api.IntegrationTests.csproj --filter "EvaluationHarness"

# Run opt-in live Gemini smoke test (requires AI:ApiKey in User Secrets)
dotnet test tests/Codexo.Api.IntegrationTests/Codexo.Api.IntegrationTests.csproj --filter "LiveGeminiSmokeTest"
```

### Common Pitfalls
- **Accidental Spoilers in Fallbacks/Mocks:** Interpolating problem titles into L1/L2 mock hints can trigger SolutionGuard failures if the problem title itself contains a data structure name (e.g. "Reverse Linked List" containing "linked list"). Keep L1/L2 mock templates purely generic.
- **Leaking Secrets via Diagnostics:** Printing configuration values or raw exception bodies can expose API keys or bearer tokens in logs. Always sanitize log output to token counts only and restrict diagnostic checks to boolean presence flags.
