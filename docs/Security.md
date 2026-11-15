# Codexo — Security

> **Status:** NOT IMPLEMENTED (stub created in M0)

## Principles

1. **Never trust the client.** The server is authoritative for hint levels, attempt state, and session management.
2. **Minimum permissions.** The Chrome extension requests only the permissions it needs.
3. **Secrets stay server-side.** API keys, JWT secrets, and connection strings are never in the repository or client code.
4. **AI output is untrusted.** Every AI response is validated before delivery to the user.
5. **Page content is untrusted.** Extracted problem text is sanitized and must never override system behavior (prompt-injection defense).

## Secret Management

| Environment | Method |
|-------------|--------|
| Local dev   | .NET User Secrets |
| Production  | Environment variables / Azure Key Vault |

## Data Collection & Retention Policy

*To be defined as features are implemented. Only data with a defined purpose will be collected.*

## Chrome Extension Permissions

*To be documented in M9 with the minimum-permission manifest.*

## Security Checklist (per module)

- [ ] All input validated
- [ ] No secrets in code or logs
- [ ] Extension trust boundary enforced
- [ ] AI credentials server-side only
- [ ] Minimum Chrome permissions
- [ ] No tokens or secrets in logs
- [ ] Data collected only with defined purpose
