# Codexo — API Documentation

> **Status:** IMPLEMENTED (M3 Authentication, M4 Problems & Sessions, M6 Hint Orchestration & Delivery, M7 Feedback & Preferences, M8 Hardening & Health)

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

### 3. Hint Orchestration Endpoints (M6)

All hint endpoints are secured with `[Authorize]` and rate-limited via the `"HintPolicy"` fixed-window policy (30 requests/minute per authenticated user).

---

#### Request Next Hint
Requests the next progressive hint for an active session. The server is strictly authoritative:
- **L1 (Understand):** Unlocked immediately upon session intake without requiring an attempt.
- **L2 through L5:** Unlocked ONLY if a valid written attempt for that level has been submitted and approved.
- Concurrency: Multiple concurrent requests for the same session coalesce safely via single-flight execution, returning the identical hint without duplicate AI tokens or advancing levels twice.

- **Method:** `POST`
- **Route:** `/api/sessions/{sessionId}/hints`
- **Authentication:** Required (`Bearer <token>`)
- **Rate Limit:** 30 requests/minute
- **Status:** IMPLEMENTED

##### Response (200 OK)
```json
{
  "id": "e3b0c442-98fc-1c14-9afb-4c8996fb9242",
  "sessionId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "level": 1,
  "hint": "Analyze the array bounds and consider whether any pair of numbers sum to target.",
  "guidingQuestions": [
    "What is the maximum time complexity acceptable given the input size?",
    "Can the same array element be used twice?"
  ],
  "concepts": [],
  "steps": [],
  "pitfalls": [],
  "isFallback": false,
  "createdAt": "2026-11-16T12:30:00.0000000Z"
}
```

##### Response (400 Bad Request)
Returned if the target hint level requires a written attempt that has not yet been submitted or approved.
```json
{
  "message": "Level L2_Direction is locked. A meaningful written attempt is required before unlocking this hint."
}
```

##### Response (404 Not Found)
Returned if the session ID does not exist or does not belong to the calling user.

##### Response (429 Too Many Requests)
Returned if Gemini quota or server rate limit is reached. Includes `Retry-After` header when available.

---

#### Submit Problem-Solving Attempt
Submits the learner's written attempt for validation via `AttemptGatingValidator`. If approved, unlocks the next level.

- **Method:** `POST`
- **Route:** `/api/sessions/{sessionId}/attempts`
- **Authentication:** Required (`Bearer <token>`)
- **Status:** IMPLEMENTED

##### Request Body
```json
{
  "attemptText": "For each number in nums, compute complement = target - nums[i]. Check if complement exists in a hash map."
}
```

##### Validation Rules
- `attemptText`: Required, 15 to 5,000 characters, minimum 3 distinct words.
- Rejects empty, keysmash (e.g. `"asdfasdfasdf"`), repetitive character floods, trivial phrases (`"idk"`, `"help"`), or full solution begging (`"give me the full code"`).

##### Response (200 OK — Attempt Approved)
```json
{
  "attemptId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "unlocked": true,
  "targetLevel": 2,
  "status": 1,
  "rejectionReason": null,
  "feedbackMessage": "Great reflection! Your attempt meets the criteria to unlock the next hint level."
}
```

##### Response (200 OK — Attempt Rejected)
```json
{
  "attemptId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "unlocked": false,
  "targetLevel": 2,
  "status": 2,
  "rejectionReason": "FullSolutionSolicitation",
  "feedbackMessage": "Codexo does not provide full solutions. Write a line describing what you have tried or what you understand about the problem constraints."
}
```

---

#### Get All Hints for Session
Retrieves the complete chronological list of unlocked hints for the given session.

- **Method:** `GET`
- **Route:** `/api/sessions/{sessionId}/hints`
- **Authentication:** Required (`Bearer <token>`)
- **Status:** IMPLEMENTED

##### Response (200 OK)
Returns array of `HintResponseDto`.

---

#### Get Hint Progression Status
Retrieves current progression ladder status, indicating whether the learner can request the next hint or needs to submit an attempt.

- **Method:** `GET`
- **Route:** `/api/sessions/{sessionId}/hints/status`
- **Authentication:** Required (`Bearer <token>`)
- **Status:** IMPLEMENTED

##### Response (200 OK)
```json
{
  "sessionId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "currentLevel": 1,
  "canRequestNextHint": false,
  "nextLevel": 2,
  "attemptRequiredForNextLevel": true,
  "reason": "A valid attempt is required to unlock L2_Direction."
}
```

---

### 4. Feedback & Preferences Endpoints (M7)

All endpoints require authentication via JWT Bearer token and enforce strict per-user tenant isolation.

---

#### Submit Hint Feedback
Submits helpfulness feedback (thumbs up/down) and an optional qualitative comment (max 500 characters) for a specific hint.
- **Idempotent / Upsert:** One feedback record per user per hint. If the user already submitted feedback for this hint, their rating and comment are updated.
- **Tenant Validation:** Verified that the hint belongs to a session owned by the authenticated caller.

- **Method:** `POST`
- **Route:** `/api/hints/{hintId}/feedback`
- **Authentication:** Required (`Bearer <token>`)
- **Status:** IMPLEMENTED

##### Request Body
```json
{
  "isHelpful": true,
  "comment": "The guiding questions helped me focus on array bounds without giving away the approach."
}
```

##### Validation Rules
- `isHelpful`: Required (boolean).
- `comment`: Optional, maximum 500 characters.

##### Response (200 OK)
```json
{
  "id": "c71a324b-3241-4567-89ab-cdef01234567",
  "hintId": "e3b0c442-98fc-1c14-9afb-4c8996fb9242",
  "userId": "d748f3e2-1234-4567-89ab-cdef01234567",
  "isHelpful": true,
  "comment": "The guiding questions helped me focus on array bounds without giving away the approach.",
  "createdAt": "2026-11-16T12:44:00.0000000Z"
}
```

##### Response (404 Not Found)
Returned if the hint does not exist or belongs to another user's session.

---

#### Get User Feedback for Hint
Retrieves the calling user's feedback for a specific hint if previously submitted.

- **Method:** `GET`
- **Route:** `/api/hints/{hintId}/feedback`
- **Authentication:** Required (`Bearer <token>`)
- **Status:** IMPLEMENTED

##### Response (200 OK)
Returns `HintFeedbackResponseDto`.

##### Response (404 Not Found)
Returned if no feedback has been submitted by this user for the specified hint.

---

#### Get User Study Preferences
Retrieves user study preferences (preferred programming language, auto-unlock level 1). If no preferences record exists yet for the user, default settings (`PreferredLanguage = "C#"`, `AutoUnlockL1 = true`) are created and returned.

- **Method:** `GET`
- **Route:** `/api/preferences`
- **Authentication:** Required (`Bearer <token>`)
- **Status:** IMPLEMENTED

##### Response (200 OK)
```json
{
  "id": "f81d4fae-7dec-11d0-a765-00a0c91e6bf6",
  "userId": "d748f3e2-1234-4567-89ab-cdef01234567",
  "preferredLanguage": "C#",
  "autoUnlockL1": true,
  "updatedAt": "2026-11-16T12:44:00.0000000Z"
}
```

---

#### Update User Study Preferences
Upserts the authenticated user's study preferences.

- **Method:** `PUT`
- **Route:** `/api/preferences`
- **Authentication:** Required (`Bearer <token>`)
- **Status:** IMPLEMENTED

##### Request Body
```json
{
  "preferredLanguage": "Python",
  "autoUnlockL1": false
}
```

##### Validation Rules
- `preferredLanguage`: Required, maximum 50 characters.
- `autoUnlockL1`: Required (boolean).

##### Response (200 OK)
Returns updated `UserPreferenceResponseDto`.

---

### 5. Hardening & Health Monitoring Endpoints (M8)

---

#### System Health Check
Anonymous health check reporting database connectivity and AI provider availability. Strictly checks presence without ever leaking API keys, connection strings, or secret values.

- **Method:** `GET`
- **Route:** `/health`
- **Authentication:** Anonymous (`[AllowAnonymous]`)
- **Status:** IMPLEMENTED

##### Response (200 OK — Healthy)
```json
{
  "status": "Healthy",
  "timestamp": "2026-11-16T12:50:00.0000000Z",
  "database": "Connected",
  "ai": {
    "provider": "Fake",
    "model": "gemini-4-hydrogen",
    "configured": true
  }
}
```

##### Response (503 Service Unavailable — Unhealthy)
Returned if SQL Server connectivity fails or AI provider is unconfigured.

---

### 6. Standardized Error Handling (RFC 7807)

All non-success responses emitted by the application or unhandled exception middleware adhere to the **RFC 7807 Problem Details** specification (`application/problem+json`):

```json
{
  "status": 404,
  "title": "Resource Not Found",
  "detail": "Session 12345678-1234-1234-1234-123456789abc was not found for the authenticated user.",
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "instance": "/api/sessions/12345678-1234-1234-1234-123456789abc"
}
```

- **400 Bad Request:** Validation failure, empty attempt, locked level.
- **401 Unauthorized:** Missing or expired JWT Bearer token.
- **404 Not Found:** Entity does not exist or belongs to another user (anti-enumeration).
- **429 Too Many Requests:** Fixed-window rate limit (6 requests/minute) exceeded. Always includes `Retry-After: 60`.
- **500 Internal Server Error:** Sanitized server error. Full traces logged to server logs; never sent to browser.
- **503 Service Unavailable:** AI provider or database connectivity failure.
