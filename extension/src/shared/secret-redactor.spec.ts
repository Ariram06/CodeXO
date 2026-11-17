import { describe, it, expect } from 'vitest';
import { redactSecrets, REDACTED_MASK } from './secret-redactor';

describe('SecretRedactor (Client-Side)', () => {
  it('should return empty string for null, undefined or empty input', () => {
    expect(redactSecrets(null)).toBe('');
    expect(redactSecrets(undefined)).toBe('');
    expect(redactSecrets('')).toBe('');
  });

  it('should leave innocent text intact', () => {
    const code = 'const sum = (a: number, b: number) => a + b;\nconsole.log(sum(1, 2));';
    expect(redactSecrets(code)).toBe(code);
  });

  it('should redact Google API keys (AIza...)', () => {
    const text = 'const key = "AIzaSyD-1234567890abcdefghijklmnopqrst";';
    const redacted = redactSecrets(text);
    expect(redacted).not.toContain('AIzaSyD');
    expect(redacted).toContain(REDACTED_MASK);
  });

  it('should redact OpenAI style secret keys (sk-...) and GitHub tokens (ghp_...)', () => {
    const text = 'openai_key = "sk-abcdef1234567890abcdef1234567890"\ngh_token = "ghp_123456789012345678901234567890123456"';
    const redacted = redactSecrets(text);
    expect(redacted).not.toContain('sk-abcdef');
    expect(redacted).not.toContain('ghp_12345');
    expect(redacted).toContain(REDACTED_MASK);
  });

  it('should redact RSA and EC private key blocks', () => {
    const keyBlock = `-----BEGIN RSA PRIVATE KEY-----
MIIEowIBAAKCAQEA0Y1+example+key+data+here
-----END RSA PRIVATE KEY-----`;
    const redacted = redactSecrets(keyBlock);
    expect(redacted).toBe(REDACTED_MASK);
  });

  it('should redact Authorization Bearer tokens', () => {
    const header = 'Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.xyz.abc';
    const redacted = redactSecrets(header);
    expect(redacted).toBe(`Authorization: Bearer ${REDACTED_MASK}`);
  });

  it('should redact inline password and secret assignments', () => {
    const input = 'const password = "SuperSecretPassword123!"; const api_key: "my_secret_key";';
    const redacted = redactSecrets(input);
    expect(redacted).not.toContain('SuperSecretPassword123!');
    expect(redacted).not.toContain('my_secret_key');
    expect(redacted).toContain(REDACTED_MASK);
  });
});
