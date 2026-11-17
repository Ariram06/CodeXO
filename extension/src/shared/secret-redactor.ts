// Codexo Client-Side Secret Redactor
// Strips API keys, private keys, tokens, and password assignments
// in the browser before sending data over the wire (defense-in-depth).

export const REDACTED_MASK = '[REDACTED_SECRET]';

const PRIVATE_KEY_BLOCK_REGEX = /-----BEGIN (?:[A-Z ]+ )?PRIVATE KEY-----[\s\S]*?-----END (?:[A-Z ]+ )?PRIVATE KEY-----/g;
const GOOGLE_API_KEY_REGEX = /\bAIza[0-9A-Za-z\-_]{30,40}\b/g;
const GENERIC_API_KEY_REGEX = /\b(sk-[a-zA-Z0-9_\-]{20,}|ghp_[a-zA-Z0-9]{36}|glpat-[a-zA-Z0-9\-=_]{20,})\b/g;
const BEARER_TOKEN_REGEX = /\bBearer\s+[a-zA-Z0-9\-._~+/]+=*/gi;
const PASSWORD_ASSIGNMENT_REGEX = /\b(password|passwd|pwd|secret|api_?key)\s*([:=])\s*["']?([^ \t\r\n"';,]+)["']?/gi;

export function redactSecrets(text?: string | null): string {
  if (!text) {
    return text ?? '';
  }

  let result = text;
  result = result.replace(PRIVATE_KEY_BLOCK_REGEX, REDACTED_MASK);
  result = result.replace(GOOGLE_API_KEY_REGEX, REDACTED_MASK);
  result = result.replace(GENERIC_API_KEY_REGEX, REDACTED_MASK);
  result = result.replace(BEARER_TOKEN_REGEX, `Bearer ${REDACTED_MASK}`);
  result = result.replace(PASSWORD_ASSIGNMENT_REGEX, (_match, keyName, delimiter) => {
    return `${keyName}${delimiter}"${REDACTED_MASK}"`;
  });

  return result;
}
