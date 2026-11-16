# Codexo — API Documentation

> **Status:** PARTIALLY IMPLEMENTED (M3 Authentication and M4 Problems + Sessions complete; hint endpoints to be documented in M5–M7)

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

---

## Endpoints

### 1. Auth Endpoints

#### Register User
- **Method:** `POST`
- **Route:** `/api/auth/register`
- **Authentication:** None (Anonymous)
- **Status:** IMPLEMENTED

##### Request Body
```json
{
  "email": "user@example.com",
  "password": "SecurePassword123!",
  "confirmPassword": "SecurePassword123!"
}
```

##### Response (200 OK)
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAtUtc": "2026-11-16T12:15:00.0000000Z",
  "userId": "d748f3e2-1234-4567-89ab-cdef01234567",
  "email": "user@example.com"
}
```

#### Login User
- **Method:** `POST`
- **Route:** `/api/auth/login`
- **Authentication:** None (Anonymous)
- **Status:** IMPLEMENTED

##### Request Body
```json
{
  "email": "user@example.com",
  "password": "SecurePassword123!"
}
```

##### Response (200 OK)
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAtUtc": "2026-11-16T12:15:00.0000000Z",
  "userId": "d748f3e2-1234-4567-89ab-cdef01234567",
  "email": "user@example.com"
}
```

#### Current User Profile
- **Method:** `GET`
- **Route:** `/api/auth/me`
- **Authentication:** Required (`Bearer <token>`)
- **Status:** IMPLEMENTED

##### Response (200 OK)
```json
{
  "userId": "d748f3e2-1234-4567-89ab-cdef01234567",
  "email": "user@example.com"
}
```

---

### 2. Sessions Endpoints (M4)

#### Intake Problem Context (Get or Create Session)
Ingests extracted webpage problem data, deduplicates/upserts the problem in the catalog, and returns the calling user's active session.

- **Method:** `POST`
- **Route:** `/api/sessions/context`
- **Authentication:** Required (`Bearer <token>`)
- **Status:** IMPLEMENTED

##### Request Body
```json
{
  "platform": 1,
  "platformProblemId": "1",
  "title": "Two Sum",
  "slug": "two-sum",
  "url": "https://leetcode.com/problems/two-sum/",
  "difficulty": "Easy",
  "description": "Given an array of integers nums and an integer target, return indices of the two numbers such that they add up to target."
}
```

##### Validation Rules
- `platform`: Required, valid DSA platform enum (`1` for LeetCode).
- `platformProblemId`: Required, 1–100 characters.
- `title`: Required, 1–256 characters.
- `slug`: Required, 1–256 characters (alphanumeric, dashes, underscores).
- `url`: Required, valid absolute HTTP/HTTPS URL $\le$ 2048 characters.
- `description`: Required, 10–20,000 characters.
- `difficulty`: Optional, $\le$ 50 characters.

##### Response (200 OK)
```json
{
  "sessionId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "userId": "d748f3e2-1234-4567-89ab-cdef01234567",
  "problem": {
    "id": "11111111-1111-1111-1111-111111111111",
    "platform": 1,
    "platformProblemId": "1",
    "title": "Two Sum",
    "slug": "two-sum",
    "url": "https://leetcode.com/problems/two-sum/",
    "description": "Given an array of integers nums...",
    "difficulty": "Easy"
  },
  "currentLevel": 1,
  "status": 1,
  "startedAt": "2026-11-16T11:25:00.0000000Z",
  "lastActivityAt": "2026-11-16T11:25:00.0000000Z"
}
```

---

#### Get Session By ID
Retrieves session details by ID. Strictly filtered by the authenticated user's ID.

- **Method:** `GET`
- **Route:** `/api/sessions/{id}`
- **Authentication:** Required (`Bearer <token>`)
- **Status:** IMPLEMENTED

##### Response (200 OK)
Returns `ProblemSessionResponse`.

##### Response (404 Not Found)
Returned if the session does not exist **or belongs to another user** (preventing ID enumeration).

---

#### Get Active Session For Problem
Queries whether the calling user currently has an active session for a specific problem.

- **Method:** `GET`
- **Route:** `/api/sessions/active?platform=1&platformProblemId=1`
- **Authentication:** Required (`Bearer <token>`)
- **Status:** IMPLEMENTED

##### Query Parameters
- `platform`: Integer (e.g. `1` for LeetCode)
- `platformProblemId`: String (e.g. `"1"`)

##### Response (200 OK)
Returns `ProblemSessionResponse`.

##### Response (404 Not Found)
Returned if no active session exists for this user on the specified problem.

---

## Future Endpoints
- **AI Hints:** NOT IMPLEMENTED (M5 & M6)
- **Feedback & Preferences:** NOT IMPLEMENTED (M7)
