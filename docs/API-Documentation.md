# Codexo — API Documentation

> **Status:** PARTIALLY IMPLEMENTED (M3 Authentication complete; problem and hint endpoints to be documented in M4–M7)

## Base URL

`https://localhost:5001/api`

---

## Authentication

Codexo APIs are secured via **JSON Web Tokens (JWT)** using the HTTP `Authorization` header:

```http
Authorization: Bearer <jwt-token>
```

### Token Specification
- **Algorithm:** HMAC-SHA256 (`HS256`)
- **Key Storage:** .NET User Secrets in development (`Jwt:Key`), Azure Key Vault / Environment Variables in production. Keys MUST be at least 256 bits (32 bytes).
- **Lifetime:** Short-lived access token (60 minutes).
- **Issuer (`iss`):** `CodexoApi`
- **Audience (`aud`):** `CodexoExtension`
- **Claims:**
  - `sub` / `NameIdentifier`: User GUID (`user.Id`).
  - `email` / `Email`: User email address.
  - `jti`: Unique token ID (GUID).

### Refresh Token Architecture (PROPOSED / Design-Only)
In accordance with Codexo architecture principles, refresh tokens are currently designed for future implementation:
- **Storage:** Secure database table `UserRefreshTokens` with columns: `Id` (GUID), `UserId` (FK to `AspNetUsers`), `TokenHash` (SHA-256 hash of random 64-byte crypto string), `ExpiresAtUtc`, `RevokedAtUtc`, `ReplacedByTokenId`.
- **Rotation:** Refresh token rotation on each exchange. Reusing a revoked token invalidates the entire user token family immediately (theft detection).
- **Transport:** Delivered via `HttpOnly`, `SameSite=Strict`, `Secure` cookie or side panel extension secure storage.

---

## Endpoints

### 1. Register User

Creates a new user account and returns an authentication token.

- **Method:** `POST`
- **Route:** `/api/auth/register`
- **Authentication:** None (Anonymous)
- **Status:** IMPLEMENTED

#### Request Body
```json
{
  "email": "user@example.com",
  "password": "SecurePassword123!",
  "confirmPassword": "SecurePassword123!"
}
```

#### Validation Rules
- `email`: Required, valid email format, max 256 characters.
- `password`: Required, minimum 8 characters, at least 1 uppercase, 1 lowercase, 1 digit, 1 special character.
- `confirmPassword`: Required, must exactly match `password`.

#### Response (200 OK)
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAtUtc": "2026-11-16T12:15:00.0000000Z",
  "userId": "d748f3e2-1234-4567-89ab-cdef01234567",
  "email": "user@example.com"
}
```

#### Response (400 Bad Request)
Returned when input validation fails or if registration is rejected (account enumeration defense):
```json
{
  "errors": [
    "Password must be at least 8 characters long."
  ]
}
```

---

### 2. Login User

Authenticates credentials and returns a signed JWT.

- **Method:** `POST`
- **Route:** `/api/auth/login`
- **Authentication:** None (Anonymous)
- **Status:** IMPLEMENTED

#### Request Body
```json
{
  "email": "user@example.com",
  "password": "SecurePassword123!"
}
```

#### Response (200 OK)
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAtUtc": "2026-11-16T12:15:00.0000000Z",
  "userId": "d748f3e2-1234-4567-89ab-cdef01234567",
  "email": "user@example.com"
}
```

#### Response (401 Unauthorized)
Returned uniformly whether the email does not exist or the password is incorrect (account enumeration defense):
```json
{
  "message": "Invalid email or password."
}
```

---

### 3. Current User Profile

Retrieves the authenticated user's identity details from validated claims.

- **Method:** `GET`
- **Route:** `/api/auth/me`
- **Authentication:** Required (`Bearer <token>`)
- **Status:** IMPLEMENTED

#### Response (200 OK)
```json
{
  "userId": "d748f3e2-1234-4567-89ab-cdef01234567",
  "email": "user@example.com"
}
```

#### Response (401 Unauthorized)
Returned if the `Authorization` header is missing, malformed, expired, or cryptographically tampered.

---

## Future Endpoints
- **Problems & Sessions:** NOT IMPLEMENTED (M4)
- **AI Hints:** NOT IMPLEMENTED (M5 & M6)
- **Feedback & Preferences:** NOT IMPLEMENTED (M7)
