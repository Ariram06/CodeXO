# Codexo — Clean-State Test Report

> Generated: 2026-11-16T14:28:00+05:30  
> Branch: `chore/cleanup-and-verify`  
> Environment: Windows (PowerShell 5.1), .NET 9.0 SDK, Node.js v22.13.0, npm 10.9.2  
> Overall Status: **ALL CHECKS PASSED (161/161 automated tests green)**

---

## 1. Test Summary Totals

| Test Suite | Total Tests | Passed | Failed | Skipped | Status |
|:---|:---:|:---:|:---:|:---:|:---:|
| **Codexo.Core.Tests** | 86 | 86 | 0 | 0 | **PASSED** |
| **Codexo.Api.IntegrationTests** | 50 | 50 | 0 | 0 | **PASSED** |
| **Extension Tests (Vitest / Happy-DOM)** | 25 | 25 | 0 | 0 | **PASSED** |
| **Total Automated Tests** | **161** | **161** | **0** | **0** | **PASSED** |

---

## 2. Clean-State Verification Execution

### Step 1: Backend Clean, Restore, Release Build
- **Command:** `dotnet clean Codexo.slnx -c Release`
- **Result:** PASSED
```
Clean succeeded.
    0 Warning(s)
    0 Error(s)
```
- **Command:** `dotnet restore Codexo.slnx`
- **Result:** PASSED
```
Determining projects to restore...
Restored C:\Ariram\CodeXO\src\Codexo.Core\Codexo.Core.csproj
Restored C:\Ariram\CodeXO\src\Codexo.Infrastructure\Codexo.Infrastructure.csproj
Restored C:\Ariram\CodeXO\src\Codexo.Api\Codexo.Api.csproj
Restored C:\Ariram\CodeXO\tests\Codexo.Core.Tests\Codexo.Core.Tests.csproj
Restored C:\Ariram\CodeXO\tests\Codexo.Api.IntegrationTests\Codexo.Api.IntegrationTests.csproj
```
- **Command:** `dotnet build Codexo.slnx -c Release --no-restore`
- **Result:** PASSED (0 Warnings, 0 Errors)
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:03.54
```

---

### Step 2: Backend Test Execution
- **Command:** `dotnet test Codexo.slnx -c Release --no-build`
- **Result:** PASSED (136 passed, 0 failed, 0 skipped)
```
Passed!  - Failed:     0, Passed:    86, Skipped:     0, Total:    86, Duration: 554 ms - Codexo.Core.Tests.dll (net9.0)
Passed!  - Failed:     0, Passed:    50, Skipped:     0, Total:    50, Duration: 8 s - Codexo.Api.IntegrationTests.dll (net9.0)
```

---

### Step 3: Chrome Extension Clean Install, Tests, and Packaging
- **Command:** `npm --prefix extension ci`
- **Result:** PASSED (clean install from package-lock.json)
```
added 297 packages, and audited 298 packages in 26s
found 0 vulnerabilities
```
- **Command:** `npm --prefix extension test -- --watch=false`
- **Result:** PASSED (25 passed, 7 test suites)
```
 ✓ src/sidepanel/app/services/session.service.spec.ts (4 tests) 11ms
 ✓ src/sidepanel/app/services/api.service.spec.ts (4 tests) 13ms
 ✓ src/sidepanel/app/services/auth.service.spec.ts (4 tests) 12ms
 ✓ src/background/service-worker.spec.ts (4 tests) 14ms
 ✓ src/content/content-script.spec.ts (4 tests) 14ms
 ✓ src/sidepanel/app/app.component.spec.ts (2 tests) 29ms
 ✓ src/tests/integration.spec.ts (3 tests) 41ms

 Test Files  7 passed (7)
      Tests  25 passed (25)
   Start at  14:16:35
   Duration  752ms
```
- **Command:** `npm --prefix extension run package`
- **Result:** PASSED
  - Package ZIP created: `extension/dist/codexo-extension-v0.1.0.zip`
  - Size: **88.1 KB** (90,217 bytes)
- **Manifest & Bundle Verification:**
  - Manifest version: 3 (`manifest_version: 3`)
  - Permissions: `["sidePanel", "activeTab"]` (No `<all_urls>` permission, minimal least privilege)
  - Host permissions: `["*://*.leetcode.com/*", "http://localhost:5275/*", "https://localhost:7198/*"]`
  - Bundles:
    - Side panel: `dist/sidepanel/index.html` + `dist/sidepanel/main.js` + `dist/sidepanel/styles.css`
    - Content script: `dist/content/content.js`
    - Service worker: `dist/background/background.js`

---

### Step 4: Live API Server Startup & Health Check
- **Command:** `dotnet run --project src/Codexo.Api --launch-profile http` (in background)
- **Startup Presence Check (from API console logs):**
  - `[Startup Check] JWT signing key configured: yes`
  - `[Startup Check] AI provider: Fake, API key configured: yes`
- **Presence-Only Check for Gemini API Key:**
  - Command: `$hasKey = if ([string]::IsNullOrWhiteSpace($env:AISettings__ApiKey) -and [string]::IsNullOrWhiteSpace($env:GEMINI_API_KEY)) { 'no' } else { 'yes' }; "Gemini key present: $hasKey"`
  - Output: `Gemini key present: no`
  - Note: Evaluated via presence-only check; key value was never read, printed, or accessed. AI Provider runs in `Fake` mode as required.
- **Health Endpoint Check:**
  - Command: `Invoke-RestMethod -Uri "http://localhost:5275/health" -Method Get`
  - Output:
```json
{
  "status": "Healthy",
  "timestamp": "2026-11-16T08:54:43.9350084+00:00",
  "database": "Connected",
  "ai": {
    "provider": "Fake",
    "model": "gemini-4-hydrogen",
    "configured": true
  }
}
```

---

### Step 5: End-to-End Flow Verification (PowerShell Invoke-RestMethod)

All tokens kept in variables and masked from console output per rules.

1. **User Registration:**
   - Endpoint: `POST http://localhost:5275/api/auth/register`
   - Payload: `{"email": "verify_4b0669df@example.com", "password": "VerifyPass123!", "confirmPassword": "VerifyPass123!"}`
   - Result: **HTTP 200 OK** (`userId: c82b0a26-9a9c-4b11-8302-01d0b83663e3`)

2. **User Login:**
   - Endpoint: `POST http://localhost:5275/api/auth/login`
   - Payload: `{"email": "verify_4b0669df@example.com", "password": "VerifyPass123!"}`
   - Result: **HTTP 200 OK** (JWT token length: 611 characters; value masked)

3. **Unauthenticated Access (Protected Endpoint):**
   - Endpoint: `GET http://localhost:5275/api/preferences` (no auth header)
   - Result: **HTTP 401 Unauthorized** (PASSED)

4. **Authenticated Access (Protected Endpoint):**
   - Endpoint: `GET http://localhost:5275/api/preferences` (`Authorization: Bearer <token>`)
   - Result: **HTTP 200 OK** (Default preferences: `preferredLanguage: C#`, `autoUnlockLevel1: false`) (PASSED)

5. **Problem Intake / Session Creation (Seeded Two Sum):**
   - Endpoint: `POST http://localhost:5275/api/sessions/context`
   - Payload:
```json
{
  "platform": 1,
  "platformProblemId": "1",
  "title": "Two Sum",
  "slug": "two-sum",
  "url": "https://leetcode.com/problems/two-sum/",
  "description": "Given an array of integers nums and an integer target, return indices of the two numbers such that they add up to target.",
  "difficulty": "Easy"
}
```
   - Result: **HTTP 200 OK** (`sessionId: 285c6748-80aa-482f-9864-502ef42e9b24`, `currentLevel: 1`) (PASSED)

6. **Request Level 1 Hint:**
   - Endpoint: `POST http://localhost:5275/api/sessions/285c6748-80aa-482f-9864-502ef42e9b24/hints`
   - Result: **HTTP 200 OK** (`level: 1`, `hintId: 345d8f72-7337-48a9-b570-7410fdd491dc`) (PASSED)

7. **Premature Level 2 Hint Request (Gating Check):**
   - Endpoint: `POST http://localhost:5275/api/sessions/285c6748-80aa-482f-9864-502ef42e9b24/hints`
   - Result: **HTTP 400 Bad Request** (`message: "An accepted attempt is required before unlocking hint level 2."`) (PASSED)

8. **Submit Genuine Problem-Solving Attempt:**
   - Endpoint: `POST http://localhost:5275/api/sessions/285c6748-80aa-482f-9864-502ef42e9b24/attempts`
   - Payload: `{"attemptText": "I tried using a hash map to store the complements as we iterate through nums."}`
   - Result: **HTTP 200 OK** (`unlocked: true`, `status: Accepted`, `feedbackMessage: "Attempt approved."`) (PASSED)

9. **Request Level 2 Hint (After Approved Attempt):**
   - Endpoint: `POST http://localhost:5275/api/sessions/285c6748-80aa-482f-9864-502ef42e9b24/hints`
   - Result: **HTTP 200 OK** (`level: 2`, `hintId: 668cba8b-15c5-4781-9a0d-2e0810a04754`) (PASSED)

10. **Submit Hint Helpfulness Feedback:**
    - Endpoint: `POST http://localhost:5275/api/hints/668cba8b-15c5-4781-9a0d-2e0810a04754/feedback`
    - Payload: `{"isHelpful": true, "comment": "The L2 hint helped clarify the dictionary lookup approach."}`
    - Result: **HTTP 200 OK** (`isHelpful: true`, `comment: "The L2 hint helped clarify the dictionary lookup approach."`) (PASSED)

---

### Step 6: Negative Checks

1. **Validation Rejection (Bad Input):**
   - Endpoint: `POST http://localhost:5275/api/auth/register`
   - Payload: `{"email": "not-an-email", "password": "short", "confirmPassword": "diff"}`
   - Result: **HTTP 400 Bad Request** (validation errors caught for email format, password complexity, and confirmation match) (PASSED)

2. **Full Solution Solicitation Rejection:**
   - Endpoint: `POST http://localhost:5275/api/sessions/285c6748-80aa-482f-9864-502ef42e9b24/attempts`
   - Payload: `{"attemptText": "give me the full code and solution please"}`
   - Result: **HTTP 200 OK** with pedagogical rejection:
     - `unlocked: false`
     - `status: Rejected`
     - `rejectionReason: FullSolutionSolicitation`
     - `feedbackMessage: "Codexo does not provide full solutions. Write a line describing what you have tried or what you understand about the problem constraints."` (PASSED)

3. **Per-User Rate Limiting:**
   - Endpoint: `POST http://localhost:5275/api/sessions/285c6748-80aa-482f-9864-502ef42e9b24/hints`
   - Mechanism: Repeated rapid requests within 1 minute window (PermitLimit = 6/min).
   - Result: **HTTP 429 Too Many Requests** triggered on request iteration 4 (subsequent to previous hint requests in same test window), with `Retry-After: 60` response header and `application/problem+json` payload (PASSED).

---

### Step 7: Teardown & Port Cleanliness
- **Command:** Background process terminated.
- **Port Check:** `Get-NetTCPConnection -LocalPort 5275 -ErrorAction SilentlyContinue`
- **Result:** No active connections; port 5275 is completely clean. (PASSED)
