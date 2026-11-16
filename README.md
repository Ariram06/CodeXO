# Codexo

> **A Chrome extension that reads DSA problems and provides progressive, problem-specific hints — never the full solution.**  
> Built with clean architecture on **ASP.NET Core (.NET 10)**, **Entity Framework Core**, **Angular 21 (Signals)**, and **Chrome Manifest V3**.

---

## 1. Pedagogical Hint Ladder

Codexo follows the principle of **minimum effective hint** and progressive disclosure. The server is authoritative for progression and attempt gating.

| Level | Name | Pedagogical Goal | Constraints & Guardrails |
| :---: | :--- | :--- | :--- |
| **L1** | **Understand** | Restate the ask, inputs/outputs, constraints, key observations | **NO** algorithm or data-structure names, no approach. |
| **L2** | **Direction** | Guiding questions, pattern clues, problem-solving mindset | **NO** algorithm names, no code syntax. |
| **L3** | **Concepts** | Data structures and algorithmic techniques with justification | **NO** code syntax or step-by-step solutions. |
| **L4** | **Steps** | 3 to 6 plain-English algorithmic steps | **NO** code syntax or language-specific keywords. |
| **L5** | **Deep** | Pitfalls, edge cases, time/space complexity trade-offs | **NO** finished solution code. |

### Core Invariants
- **Attempt-Gating**: Unlocking levels L2 through L5 requires a meaningful written attempt from the learner (at least 10 characters describing their thinking). Empty inputs, repetitive junk, and solution solicitations (*"give me the full code"*) are rejected with educational reminders.
- **SolutionGuard**: Every AI-generated response is inspected structurally (regex AST checking for code blocks, function headers, syntax loops, and spoiler keywords). On failure, 1 constrained regeneration with an explicit reprimand is attempted; if that also fails, a safe static fallback hint is returned.
- **Server Authoritative**: The server tracks hint levels and attempt states. Duplicate or parallel client requests coalesce onto a single generation task via single-flight locking, guaranteeing levels never advance twice.

---

## 2. Architecture & Tech Stack

```
Codexo/
├── src/
│   ├── Codexo.Core/                 # Pure domain: entities, enums, hint ladder, prompt templates, SolutionGuard
│   ├── Codexo.Infrastructure/       # AppDbContext, Identity, HybridCache, OpenAiCompatibleAiClient (Gemini)
│   └── Codexo.Api/                  # Controllers, RFC 7807 ExceptionMiddleware, Sanitized Logging, RateLimiter
├── tests/
│   ├── Codexo.Core.Tests/           # 86 domain and SolutionGuard unit tests
│   └── Codexo.Api.IntegrationTests/ # 50 API integration and E2E contract tests
├── extension/                       # Chrome Manifest V3 extension
│   ├── public/                      # manifest.json and icons
│   ├── scripts/                     # Standalone esbuild bundler and packaging script
│   └── src/
│       ├── background/              # Ephemeral service worker message router and tab cache
│       ├── content/                 # Resilient DOM problem extractor and SPA MutationObserver
│       ├── shared/                  # Strongly-typed extension messaging contracts
│       └── sidepanel/               # Angular 21 SPA (Standalone components, Signals, Reactive Forms)
└── docs/                            # Requirements, Architecture, Database, AI Logic, Learning Log, Run Log
```

- **Backend:** ASP.NET Core Web API (.NET 10 LTS), EF Core Code First, SQL Server (LocalDB in dev), ASP.NET Core Identity + JWT, `HybridCache` multi-tier caching, Serilog sanitized logging, RFC 7807 ProblemDetails, fixed-window rate limiting.
- **AI Integration:** Provider-independent `IAiClient` targeting Google Gemini (`gemini-4-hydrogen`) via OpenAI-compatible endpoints with `reasoning_effort: "low"` and `FakeAiClient` for 100% offline determinism.
- **Extension & Frontend:** Chrome Manifest V3, Side Panel API, standalone `esbuild` for content scripts and background service workers, Angular 21 (Signals, standalone components, Vitest test runner).

---

## 3. Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/)
- [Node.js 22+](https://nodejs.org/) and `npm 10+`
- SQL Server LocalDB (included with Visual Studio / VS Code C# Dev Kit on Windows) or SQL Server Express
- Google Chrome (v116+ for Side Panel API support)

---

### Backend Setup

1. **Restore dependencies and build solution:**
   ```bash
   dotnet restore Codexo.slnx
   dotnet build Codexo.slnx
   ```

2. **Configure Local Secrets (Never commit secrets!):**
   ```bash
   # Configure a strong JWT key (min 32 bytes)
   dotnet user-secrets set "Jwt:Key" "SuperSecretJwtSigningKeyForDev1234567890!" --project src/Codexo.Api/Codexo.Api.csproj

   # (Optional) Configure Gemini API Key for live AI generation
   dotnet user-secrets set "AI:ApiKey" "<your-gemini-api-key>" --project src/Codexo.Api/Codexo.Api.csproj
   ```

3. **Apply Database Migrations:**
   ```bash
   dotnet ef database update --project src/Codexo.Infrastructure/Codexo.Infrastructure.csproj --startup-project src/Codexo.Api/Codexo.Api.csproj
   ```

4. **Run the Web API:**
   ```bash
   dotnet run --project src/Codexo.Api/Codexo.Api.csproj
   ```
   - API listens on: `https://localhost:7001` / `http://localhost:5000`
   - Swagger OpenAPI Docs: `https://localhost:7001/swagger`
   - Health check: `https://localhost:7001/health`

---

### Extension Setup & Packaging

1. **Install dependencies:**
   ```bash
   npm --prefix extension install
   ```

2. **Build the extension:**
   ```bash
   npm --prefix extension run build
   ```
   This compiles the Angular side panel into `extension/dist/codexo/sidepanel/` and bundles `service-worker.ts` and `content-script.ts` with `esbuild`.

3. **Package the extension into a zip release artifact:**
   ```bash
   npm --prefix extension run package
   ```
   Creates `extension/dist/codexo-extension-v0.1.0.zip` ready for Chrome Web Store distribution or manual installation.

4. **Load into Google Chrome:**
   - Open Chrome and navigate to `chrome://extensions/`.
   - Enable **Developer mode** (toggle in upper right).
   - Click **Load unpacked** and select the folder: `c:\Ariram\CodeXO\extension\dist\codexo`.
   - Navigate to any problem on [LeetCode](https://leetcode.com/problems/two-sum/) and click the Codexo extension icon to open the Side Panel!

---

## 4. Running the Tests

### Backend Test Suite (136 tests)
```bash
# Run all unit tests and offline integration tests
dotnet test Codexo.slnx --verbosity normal

# Run opt-in live Gemini AI smoke test (requires AI:ApiKey)
$env:RUN_LIVE_TESTS="true"
dotnet test tests/Codexo.Api.IntegrationTests/Codexo.Api.IntegrationTests.csproj --filter "LiveGeminiSmokeTest"
```

### Extension Test Suite (25 tests)
```bash
# Run all Vitest unit and end-to-end integration tests
npm --prefix extension test -- --watch=false
```

---

## 5. Security & Rate Limiting

- **Multi-Tenant Data Isolation:** Authenticated `UserId` is extracted strictly from cryptographically verified JWT claims (`ClaimTypes.NameIdentifier`). Database queries enforce tenant ownership; cross-user attempts return `404 Not Found` to prevent ID enumeration.
- **Sanitized Request Logging:** Logs record only HTTP Method, Path, StatusCode, and Duration. Authorization headers, passwords, and user prompts are never logged.
- **Zero-Secret Health Monitoring:** Anonymous `/health` probes database connectivity and AI provider readiness without exposing API key prefixes or connection strings.
- **Configurable Rate Limiting:** Fixed-window limiter defaults to 6 hint requests per minute partitioned by user ID, protecting LLM quotas and returning RFC 7807 `429 Too Many Requests` with `Retry-After: 60`.

---

## 6. Release & CI/CD Pipeline

The GitHub Actions workflow in `.github/workflows/ci.yml` runs automatically on pushes and pull requests to `main`:
1. **Backend Job**: Sets up .NET 10, builds `Codexo.slnx`, and executes all 136 unit and integration tests.
2. **Extension Job**: Sets up Node.js 22, runs 25 Vitest tests, compiles production bundles, packages `codexo-extension-v0.1.0.zip`, and uploads the zip as a downloadable workflow artifact.

---

## 7. Documentation & Logs

- [Requirements](docs/Requirements.md)
- [Architecture & Design](docs/Architecture.md)
- [Database Schema](docs/Database-Design.md)
- [API Documentation](docs/API-Documentation.md)
- [AI Logic & Guardrails](docs/AI-Logic.md)
- [Security Spec](docs/Security.md)
- [Learning Log & Teach-backs](docs/LEARNING-LOG.md)
- [Autonomous Execution Run Log](docs/RUN-LOG.md)
