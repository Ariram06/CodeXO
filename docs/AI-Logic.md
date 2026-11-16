# Codexo — AI Logic & Hint Ladder

> **Status:** IMPLEMENTED (M1 Hint Validation & Solution Guard, M5 AI Client & Gemini Integration)

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
- Plain-English guidance (not compilable in any language)

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

## AI Client Architecture (M5)

Codexo abstracts AI generation behind a provider-independent domain interface:

- **`IAiClient`:** Domain interface with `GenerateHintAsync(AiPromptRequest request, CancellationToken ct)`.
- **`FakeAiClient`:** Deterministic offline client requiring zero network and zero API keys. Conforms to all L1–L5 schema rules and SolutionGuard constraints for fast unit testing and CI builds.
- **`OpenAiCompatibleAiClient`:** Production client connecting to Google Gemini's OpenAI-compatible endpoint (`/chat/completions`).
  - **Model:** `gemini-4-hydrogen` (fast, low cost, confirmed available on Google AI Studio).
  - **Reasoning Tokens:** Sets `reasoning_effort: "low"` to minimize latency and token billing.
  - **Structured JSON Schema:** Configured with `response_format: { type: "json_object" }` requesting fixed keys: `level`, `hint`, `guidingQuestions`, `concepts`, `steps`, `pitfalls`.
  - **Tolerant JSON Parsing (`TolerantJsonParser`):** Strips markdown fences (````json ... ````), extracts outer `{ ... }`, and initializes empty arrays for missing collection properties.
  - **Resilience & Transient Retry:** Handles transient 503 high-demand spikes with exponential backoff; maps 429 to `AiQuotaExceededException` with `Retry-After`; maps 401/403 to `AiAuthenticationException`.
  - **Zero Leakage Audit Logging:** Logs only `prompt_tokens`, `completion_tokens`, and `total_tokens`. Prompts, response text, HTTP headers, and API keys are strictly never logged.

## Prompt Engineering & Embedded Templates

- Prompt templates are embedded directly in `Codexo.Core.dll` as assembly resources (`Prompts/Templates/Base.txt`, `L1.txt` ... `L5.txt`).
- **Prompt Isolation:** User attempts, problem descriptions, and previous hint histories are encapsulated within distinct `<USER_DATA>` and `<DELIMITER>` tags, preventing user input or scraped web content from manipulating system instructions (prompt injection defense).

## Attempt-Gating & Solution Guard (M1, M5)

Every AI response is intercepted and validated structurally by `SolutionGuard` before reaching the application layer:
1. Markdown code fences (````) are rejected.
2. Compilable function declarations, loops, and variable assignments are rejected.
3. Premature spoiler terms (e.g. data structure names in L1/L2) are rejected.
4. Finished code solutions are rejected.

## Benchmark Evaluation Results (10 Problems, 50 Hints)

The M5 evaluation harness tested 10 DSA benchmark problems across all 5 hint levels against schema validation and `SolutionGuard`:
- Total hints evaluated: 50 / 50 passed (100% compliance).
- Opt-in live smoke test with `gemini-4-hydrogen`: passed in 4s with 0 spoiler terms and valid schema.
