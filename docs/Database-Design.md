# Codexo — Database Design

> **Status:** IMPLEMENTED (M2 Database, M2-CLEANUP Restrict FK, M3 ASP.NET Core Identity & User FKs)

## Entity-Relationship Diagram

```mermaid
erDiagram
    AppUser ||--o{ ProblemSession : "has sessions (Cascade)"
    AppUser ||--|| UserPreference : "has preference (Cascade)"
    AppUser ||--o{ HintFeedback : "submits feedback (Restrict)"
    Problem ||--o{ ProblemSession : "has sessions (Restrict)"
    ProblemSession ||--o{ Hint : "has hints (Cascade)"
    ProblemSession ||--o{ UserAttempt : "has attempts (Cascade)"
    Hint ||--o{ HintFeedback : "has feedback (Cascade)"

    AppUser {
        string Id PK
        string UserName
        string NormalizedUserName
        string Email
        string NormalizedEmail
        boolean EmailConfirmed
        string PasswordHash
        string SecurityStamp
        string ConcurrencyStamp
        datetime CreatedAtUtc
    }
    UserPreference {
        uuid Id PK
        string UserId FK "UK"
        string PreferredLanguage
        boolean AutoUnlockL1
        datetimeoffset CreatedAt
        datetimeoffset UpdatedAt
    }
    Problem {
        uuid Id PK
        int Platform
        string PlatformProblemId
        string Title
        string Slug
        string Url
        string Description
        string Difficulty
        datetimeoffset CreatedAt
        datetimeoffset UpdatedAt
    }
    ProblemSession {
        uuid Id PK
        string UserId FK
        uuid ProblemId FK
        int CurrentLevel
        int Status
        datetimeoffset StartedAt
        datetimeoffset LastActivityAt
    }
    Hint {
        uuid Id PK
        uuid ProblemSessionId FK
        int Level
        string Content
        string Explanation
        boolean IsFallback
        int GenerationAttempts
        datetimeoffset CreatedAt
    }
    UserAttempt {
        uuid Id PK
        uuid ProblemSessionId FK
        int TargetLevel
        string AttemptText
        int Status
        int RejectionReason
        datetimeoffset SubmittedAt
    }
    HintFeedback {
        uuid Id PK
        uuid HintId FK
        string UserId FK
        boolean IsHelpful
        string Comment
        datetimeoffset CreatedAt
    }
```

---

## Tables & Relationships

### 1. `AspNetUsers` (Identity `AppUser`)
- **Primary Key:** `Id` (`nvarchar(450)`)
- Custom property: `CreatedAtUtc` (`datetime2`, UTC)
- Manages authentication credentials, password hashes (PBKDF2/HMAC-SHA256), email confirmation, and security stamps.

### 2. `ProblemSessions`
- **Primary Key:** `Id` (`uniqueidentifier`)
- **Foreign Key to `AspNetUsers`:** `UserId` (`nvarchar(450)`, `ON DELETE CASCADE`)
- **Foreign Key to `Problems`:** `ProblemId` (`uniqueidentifier`, `ON DELETE RESTRICT`)
- **Composite Indexes:**
  - `IX_ProblemSessions_UserId_ProblemId` — fast lookup of a user's session for a specific problem.
  - `IX_ProblemSessions_UserId_Status` — query active vs completed sessions.

### 3. `UserPreferences`
- **Primary Key:** `Id` (`uniqueidentifier`)
- **Foreign Key to `AspNetUsers`:** `UserId` (`nvarchar(450)`, `ON DELETE CASCADE`)
- **Unique Index:** `IX_UserPreferences_UserId` — enforces exactly one preference profile per user.

### 4. `HintFeedbacks`
- **Primary Key:** `Id` (`uniqueidentifier`)
- **Foreign Key to `Hints`:** `HintId` (`uniqueidentifier`, `ON DELETE CASCADE`)
- **Foreign Key to `AspNetUsers`:** `UserId` (`nvarchar(450)`, `ON DELETE NO ACTION / RESTRICT`)
  - *Design Note:* SQL Server forbids multiple cascade paths to the same child table. Because deleting an `AppUser` cascades through `ProblemSessions -> Hints -> HintFeedbacks`, the direct foreign key from `AspNetUsers` to `HintFeedbacks` must specify `NO ACTION / RESTRICT`.

---

## Seed Data & Development Isolation

- In **Development**, `DbInitializer.SeedAsync()` seeds:
  - Benchmark problems: *Two Sum*, *Valid Anagram*, *Merge Two Sorted Lists*.
  - A deterministic development user: `dev@codexo.dev` (`00000000-0000-0000-0000-000000000001`).
  - Default user preference for the development user.
- In **Production**, `DbInitializer` does nothing; migrations and database schema updates are executed via deployment pipelines.
