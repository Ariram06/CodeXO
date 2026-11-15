# Codexo — AI Logic & Hint Ladder

> **Status:** NOT IMPLEMENTED (spec written in M0 as source of truth)

## Hint Ladder (Source of Truth)

Codexo uses a 5-level progressive hint system. Each level reveals more information but never provides a complete solution.

### L1 — Understand

**Purpose:** Help the user understand the problem.

**Includes:**
- Restate the ask in plain language
- Clarify input/output format
- Highlight constraints
- Note key observations from the problem statement

**Excludes:**
- ❌ Any approach or strategy
- ❌ Algorithm or data-structure names
- ❌ Pattern names

### L2 — Direction

**Purpose:** Point the user in the right direction without naming the technique.

**Includes:**
- Guiding questions ("What happens if...?", "Have you considered...?")
- Pattern clues (e.g., "Think about what changes when you move from left to right")

**Excludes:**
- ❌ Pattern or algorithm names (no "sliding window", "two pointers", etc.)
- ❌ Data structure names
- ❌ Code of any kind

### L3 — Concepts

**Purpose:** Name the relevant technique and justify why it fits.

**Includes:**
- MAY name the data structure or algorithm
- Explain why this technique applies to the problem
- Connect the technique to the problem's constraints

**Excludes:**
- ❌ Code of any kind
- ❌ Step-by-step implementation

### L4 — Steps

**Purpose:** Provide a structured plan for implementing the solution.

**Includes:**
- Ordered steps to solve the problem
- Pseudocode (not compilable in any language)

**Excludes:**
- ❌ Compilable code in any programming language
- ❌ Complete solution

### L5 — Deep

**Purpose:** Help with edge cases, complexity, and common pitfalls.

**Includes:**
- Edge cases and how to handle them
- Time and space complexity analysis
- Common mistakes and pitfalls
- Optimization opportunities

**Excludes:**
- ❌ Finished solution or compilable code

## Attempt-Gating Rules

- A user MUST submit a **meaningful written attempt** to unlock the next hint level.
- An attempt must contain at least one line describing what the user tried or understood.
- Empty, whitespace-only, or gibberish attempts are rejected.
- The server validates attempt quality; the client cannot bypass gating.

## Solution Guard

Every AI-generated hint is validated **structurally** before delivery:

1. **Code block detection:** Check for markdown code blocks, inline code with compilable statements.
2. **Compilable statement detection:** Scan for language-specific syntax patterns (function declarations, imports, variable assignments in code style).
3. **Complete-solution detection:** Check for step-by-step implementations that together form a complete answer.
4. **Keyword-only checking is NOT sufficient** — structural analysis is required.

### On Guard Failure:
1. Attempt one **constrained regeneration** with a stricter prompt.
2. Re-validate the regenerated response.
3. If still failing, return a **safe fallback hint** appropriate to the current level.

## Full-Solution Requests

If a user asks for the full solution:
- Return the next appropriate hint level instead.
- Include a polite explanation of Codexo's philosophy.
- Never comply with a direct solution request.

## Security Rules

- The server is authoritative for hint level and attempt state.
- Never trust a level sent by the browser.
- Duplicate or parallel requests must not advance a level twice.
- AI output is treated as untrusted content.
- Extracted webpage text is treated as untrusted (prompt-injection defense).
- Page content must never override hint rules.
