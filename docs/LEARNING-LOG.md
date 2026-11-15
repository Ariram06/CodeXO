# Codexo — Learning Log

## Module Status

| Module | Name | Status | Branch | Date |
|--------|------|--------|--------|------|
| M0 | Scaffolding | IMPLEMENTED | `main` | 2026-11-15 |
| M1 | Domain + Hint Logic | IMPLEMENTED | `main` | 2026-11-15 |
| M2 | Database | NOT IMPLEMENTED | — | — |
| M3 | Authentication | NOT IMPLEMENTED | — | — |
| M4 | Problems + Sessions | NOT IMPLEMENTED | — | — |
| M5 | AI Layer | NOT IMPLEMENTED | — | — |
| M6 | Hint Orchestration | NOT IMPLEMENTED | — | — |
| M7 | Feedback + Preferences | NOT IMPLEMENTED | — | — |
| M8 | Hardening | NOT IMPLEMENTED | — | — |
| M9 | Extension Scaffold | NOT IMPLEMENTED | — | — |
| M10 | Content Script | NOT IMPLEMENTED | — | — |
| M11 | Side Panel UI | NOT IMPLEMENTED | — | — |
| M12 | Integration | NOT IMPLEMENTED | — | — |
| M13 | Release | NOT IMPLEMENTED | — | — |

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
    C -->|"No (L1)"| D["Allow Hint Generation"]
    C -->|"Yes"| E["AttemptGatingValidator: Validate Written Attempt"]
    E -->|"Rejected"| F["Return Gating Error (Stay on Current Level)"]
    E -->|"Approved"| D
    D --> G["AI Provider (Raw JSON)"]
    G --> H["AiResponseValidator: Deserialize & Check Schema"]
    H -->|"Invalid JSON/Fields"| I["Flag MalformedJson"]
    H -->|"Valid"| J["SolutionGuard: Structural Inspection"]
    J -->|"Code Block / Compilable / Spoiler"| K["Trigger Regeneration / Fallback"]
    J -->|"Clean"| L["Deliver Approved Progressive Hint"]
```

1. User requests a hint for a problem session.
2. `HintLadder` determines the authorized target level based on current level and attempts.
3. For levels > L1, `AttemptGatingValidator` checks the user's attempt string.
4. If the attempt fails quality or solicitation checks, progression is halted at the current level.
5. AI hint response is parsed and schema-verified by `AiResponseValidator`.
6. `SolutionGuard` checks the text structurally for code fences, statement declarations, and level spoilers.
7. Only hints passing all guard checks are cleared for user delivery.

### Design Decisions & Rejected Alternatives
- **Decision:** Implemented `SolutionGuard` using multi-lingual structural regex tokenizers (function heads, typed assignments, loops) alongside code fence matchers.
  *Rejected alternative:* Keyword blocklists alone. Words like `for`, `in`, `map`, or `return` appear in normal English prose; searching for raw keywords produces unacceptable false positives.
- **Decision:** L1 is automatically unlockable without an attempt; L2 through L5 require a validated attempt.
  *Rejected alternative:* Requiring an attempt before L1. Users need to read and understand the problem scope before they can formulate a meaningful attempt.

### Interview Questions & Model Answers
- **Q: Why must hint level progression be calculated entirely server-side rather than accepted from the browser client?**
  *A:* Clients are inherently untrusted environments where malicious users or modified extension scripts can forge request payloads. If the client specified the target level, a user could bypass attempt-gating and jump directly to L4 pseudocode or L5 deep analysis. Server authority guarantees progressive disclosure invariants.
- **Q: How does `SolutionGuard` distinguish between pseudocode allowed at L4 and compilable code which is forbidden?**
  *A:* Compilable code exhibits concrete language syntax: formal type signatures (`public int[]`), language-specific function definitions (`def `, `fn `), variable assignment statements terminated with semicolons, and exact loop header syntax. Pseudocode uses high-level operational descriptions without compilable assignment tokens.
- **Q: Why does `AttemptGatingValidator` reject attempts soliciting a full solution?**
  *A:* Codexo's mission is minimum effective guidance so the learner can solve the next problem independently. A request asking "give me the complete Python code" is a solution-begging shortcut, not an attempt. The validator rejects it and guides the user to articulate what they understand about the problem.

### Run & Test Commands
```bash
# Run all unit tests for domain and hint logic
dotnet test tests/Codexo.Core.Tests/Codexo.Core.Tests.csproj --verbosity normal

# Run all tests across the solution
dotnet test Codexo.slnx
```

### Common Pitfalls
- **Accidental Spoilers at L1/L2:** Casual phrases like "use a hash map" or "notice the sliding window" destroy the learning curve. `SolutionGuard` strictly enforces term boundaries.
- **Top-Row Keyboard False Positives:** English words like "error" or "require" fall entirely on the QWERTY keyboard row (`qwertyuiop`). Keysmash filters must check consonant clusters rather than blindly checking keyboard row subsets.
