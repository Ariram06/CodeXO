# Codexo — Security

> **Status:** PARTIALLY IMPLEMENTED (M3 Auth, M4 Session Isolation, M5 AI Layer & Zero Secrets)

## Principles

1. **Never trust the client.** The server is authoritative for hint levels, attempt state, and session management.
2. **Minimum permissions.** The Chrome extension requests only the permissions it needs.
3. **Secrets stay server-side.** API keys, JWT secrets, and connection strings are never in the repository, logs, test output, or client code.
4. **AI output is untrusted.** Every AI response is validated structurally by `SolutionGuard` before delivery to the user.
5. **Page content is untrusted.** Extracted problem text is wrapped in `<USER_DATA>` delimiters and treated as data, never instructions (prompt-injection defense).

## Secret Management & Safe Diagnostics

| Environment | Method | Secret Keys |
|-------------|--------|-------------|
| Local dev   | .NET User Secrets | `Jwt:SigningKey`, `AI:ApiKey` |
| CI / Tests  | `FakeAiClient` (Zero Secrets / Zero Network) | None |
| Production  | Environment Variables (`AI__ApiKey`, `Jwt__SigningKey`) | Loaded into memory |

- **Zero-Secret Diagnostic Startup Check:** At API boot, `Program.cs` verifies key presence and outputs boolean presence only (`[Startup Check] AI provider: Fake, API key configured: yes/no`). Keys, hashes, or substrings are never emitted to logs or console.
- **Audit Logging Security:** `OpenAiCompatibleAiClient` logs strictly `prompt_tokens`, `completion_tokens`, and `total_tokens`. Prompts, response text, HTTP headers, and API keys are strictly never logged.

## AI Data Privacy Policy (Gemini API Free Tier)

> [!IMPORTANT]
> Free-tier prompts may be used by Google to train and improve its products, and free-tier quotas/rate-limits are visible in Google AI Studio.
> Until Codexo moves to a paid key or enterprise tier:
> - Send **ONLY** synthetic benchmark DSA problems and sample attempts during development and smoke testing.
> - **NEVER** send real user personal identifying information (PII), proprietary code, or production user telemetry to the AI provider.
> - Client extraction of web pages must sanitize user-identifying metadata before submitting problems.

## Authentication & Session Isolation (M3, M4)

- **HMAC-SHA256 JWT:** Signed with 256-bit secret stored in User Secrets / environment variables.
- **Account Enumeration Defense:** Generic error messages (`Invalid email or password`) on authentication failures.
- **Tenant Isolation:** All sessions (`ProblemSession`) are strictly isolated by `UserId`. Accessing another user's session returns HTTP 404 (preventing session ID enumeration).

## Chrome Extension Permissions

*To be finalized in M9 with the minimum-permission manifest.*

## Security Checklist

- [x] All input validated (FluentValidation on Auth, Sessions, Hints)
- [x] No secrets in code, repositories, or logs
- [x] Safe startup check emits only "yes/no"
- [x] Extension trust boundary enforced (server is authoritative for hint levels)
- [x] AI credentials server-side only
- [x] Free-tier prompt privacy boundary documented
- [ ] Minimum Chrome permissions (M9)
- [x] No tokens or secrets in logs (audit logging restricted to token counts)
