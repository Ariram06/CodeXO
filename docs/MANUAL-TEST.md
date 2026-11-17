# Codexo — Manual Verification & Testing Guide (Chat Pivot)

> Target Audience: Ariram (Repository Owner / Manual Tester)  
> Scope: Live End-to-End browser verification with real Chrome extension, live coding tabs, and real Gemini API responses.

---

## 1. Prerequisites

- **Google Chrome**: Version 116+ (supports SidePanel API).
- **.NET 10.0 SDK**: Installed and available in PATH.
- **Node.js**: v20+ and npm.
- **Gemini API Key**: A valid key configured via .NET User Secrets or environment variables.

---

## 2. Step 1: Start Backend API

1. Open PowerShell and navigate to the API directory:
   ```powershell
   cd C:\Ariram\CodeXO\src\Codexo.Api
   ```

2. Verify that your API key is configured (presence check only):
   ```powershell
   dotnet user-secrets list
   ```
   *Expected:* `AI:ApiKey = ...` or `AISettings:ApiKey = ...` is present.

3. Start the Codexo backend server on HTTP:
   ```powershell
   dotnet run --launch-profile http
   ```
   *Expected output:*
   - `[Startup Check] JWT signing key configured: yes`
   - `[Startup Check] AI provider: Fake (or OpenAiCompatible), API key configured: yes`
   - `Now listening on: http://localhost:5275`

4. Verify health in a separate browser tab:
   - Navigate to: `http://localhost:5275/health`
   - Confirm JSON response: `"status": "Healthy"`.

---

## 3. Step 2: Build & Load Extension in Chrome

1. Build the extension package:
   ```powershell
   cd C:\Ariram\CodeXO\extension
   npm run build
   ```
   *Verification:* Ensure `extension\dist\codexo` contains:
   - `manifest.json`
   - `background\service-worker.js`
   - `content\content-script.js`
   - `sidepanel\browser\index.html`
   - `icons\`

2. Open Google Chrome and navigate to:
   ```text
   chrome://extensions
   ```

3. In the top-right corner, toggle **Developer mode** to **ON**.

4. In the top-left corner, click **Load unpacked**.

5. Select the folder:
   ```text
   C:\Ariram\CodeXO\extension\dist\codexo
   ```

6. Confirm the card appears:
   - Name: **Codexo**
   - Version: **0.2.0**
   - Description: *AI coding assistant in your browser side panel.*

---

## 4. Step 3: Open Side Panel & Sign In

1. Click the Chrome **Extensions icon** (puzzle piece) in the toolbar and pin **Codexo**.
2. Click the **Codexo** icon to open the Chrome side panel.
3. Sign in with the seeded development credentials:
   - **Email:** `dev@codexo.dev`
   - **Password:** `DevPassword123!`
   *(Or click Register to create a new user account)*.
4. Verify successful sign-in:
   - The auth panel disappears.
   - The top header shows **⚡ Codexo AI Assistant** with a green **● Live** status dot.
   - The context bar displays the active tab platform (e.g. `LeetCode` or `Web`) and page title.
   - The chat area displays the empty state: *"Codexo AI Chat: Ask anything about this problem or code..."*

---

## 5. Step 4: Ask a Coding Question & Get Gemini Answer

1. In the main browser tab, navigate to any coding problem (e.g., [https://leetcode.com/problems/two-sum/](https://leetcode.com/problems/two-sum/)).
2. Observe the context bar in the side panel automatically updating with the active page title: `Two Sum - LeetCode`.
3. In the composer textarea, type:
   ```text
   What is the brute force approach vs the optimal approach for this problem?
   ```
4. Press **Enter**:
   - The message is sent immediately.
   - A user bubble appears on the right with your query.
   - A typing indicator appears on the left: *"Codexo is thinking..."*.
5. Within seconds, verify the assistant response appears:
   - Labeled with sender **Codexo** and model badge (e.g., `gemini-3.5-flash-lite`).
   - Contains a complete explanation comparing $O(n^2)$ nested loops with $O(n)$ hash table lookup.

---

## 6. Step 5: Test Composer Keyboard Shortcuts

1. In the composer textarea, type:
   ```text
   Here is my code idea:
   ```
2. Press **Shift+Enter**:
   - A newline is inserted. The message is **NOT** sent.
3. Type:
   ```text
   def solve(nums): return []
   ```
4. Press **Enter**:
   - The multi-line message sends immediately.

---

## 7. Step 6: Test Error Banner with Retry

1. **Simulate Service Interruption:**
   - Stop the backend API (`Ctrl+C` in the API terminal).
2. Type in composer:
   ```text
   How can I optimize space complexity?
   ```
3. Press **Enter**.
4. Observe the specific error banner:
   - Banner appears with a ⚠️ icon and specific message (e.g., *"Cannot connect to API server at http://localhost:5275"* or *"AI service is temporarily unavailable"*).
   - An active **Retry** button is visible.
5. **Recover:**
   - Restart the backend API (`dotnet run --launch-profile http`).
   - Click the **Retry** button in the error banner.
   - Verify the question is automatically re-submitted and the AI answer is received.

---

## 8. Step 7: Verify Context Collection & Code Extraction (Module C4)

1. **Inspect Context Bar Chips:**
   - Navigate to [https://leetcode.com/problems/two-sum/](https://leetcode.com/problems/two-sum/).
   - Inspect the context bar in the side panel:
     - `[LeetCode]` platform chip.
     - `[Two Sum - LeetCode]` (or question title) chip.
     - `[Python3]` (or your chosen language in LeetCode's language dropdown) chip.
     - `[Code: N lines]` green chip reflecting the lines in Monaco editor.
2. **Test Include/Exclude Code Toggle:**
   - Uncheck the **Include code** checkbox.
   - Observe the chip immediately turn orange: `[Code: Excluded]`.
   - Ask a question in chat: *"What are edge cases for this problem?"*.
   - Verify that answer is generated based on problem description without including your editor code.
   - Re-check **Include code**: chip returns to green `[Code: N lines]`.
3. **Test Manual Refresh & Tab Switch:**
   - Open a tab with [https://codeforces.com/](https://codeforces.com/) or another page.
   - Switch tabs back and forth: observe the platform and title chips update dynamically.
   - Click the **🔄** button: verify context refreshes instantly.

---

## 9. Step 8: Verify Real-Time Streaming & Upstream Cancellation (Module C5)

1. **Test Incremental SSE Token Streaming:**
   - In the side panel chat composer, ask a comprehensive question:
     ```text
     Provide a full breakdown of Two Sum: brute force vs hash map with code and complexities.
     ```
   - Press **Enter**.
   - Observe that words appear incrementally in real-time as tokens arrive via Server-Sent Events (`POST /api/chat/stream`), accompanied by a blinking streaming cursor (`▊`).
   - Notice the Send button transforms into an active **⏹ Stop** button during generation.

2. **Test Upstream Cancellation ("Stop Generating"):**
   - Ask another long question:
     ```text
     Write a detailed explanation of dynamic programming with 5 examples.
     ```
   - Press **Enter**.
   - After a few sentences have streamed, click the red **⏹ Stop** button.
   - Verify that streaming halts immediately.
   - Verify that all previously generated text is retained in the assistant bubble.
   - In the backend database, verify the assistant message is persisted with `[Generation stopped]` appended, confirming server-side persistence upon upstream client abort.

3. **Test Non-Streaming Fallback:**
   - If SSE connection drops or is blocked, verify the client automatically falls back to standard `POST /api/chat/messages` and displays the full response cleanly.

---

## 10. Step 9: Verify Markdown, Code Blocks, Copy & Auto-Scroll (Module C6)

1. **Test Markdown & Syntax Highlighting:**
   - Ask a question that produces code and equations:
     ```text
     Write a Python solution for Two Sum and explain the time complexity in big-O notation.
     ```
   - Verify the assistant answer renders:
     - Headings, bullet points, and bold terms formatted cleanly.
     - Mathematical expressions like $O(n)$ rendered with KaTeX typography.
     - Python code block rendered in a dark box with:
       - Header displaying `PYTHON` badge.
       - A **Copy** button in the top-right corner.
       - Syntax-highlighted tokens (keywords in blue, strings in green, functions in yellow, comments in gray).

2. **Test One-Click Code Copy:**
   - Click the **Copy** button on the code block.
   - Observe the button text transitions to **Copied!** with green feedback.
   - After 2 seconds, observe the button returns to **Copy**.
   - Paste into an editor (or composer) and verify exact indentation and line breaks are preserved.

3. **Test Smart Auto-Scroll with User Pause:**
   - Ask a question with a lengthy multi-paragraph answer.
   - While tokens are streaming in, scroll up in the message container to read an earlier line.
   - Observe that auto-scroll pauses immediately, preventing the page from yanking the viewport downwards.
   - A floating **↓ Jump to bottom** button appears.
   - Scroll back to the bottom or click the pill; verify smooth auto-scroll resumes.

4. **Test Composer Auto-Resize:**
   - In the composer textarea, type a multi-line message (using `Shift+Enter` for newlines).
   - Observe the textarea smoothly expands its height from 1 line up to 5 lines.
   - Paste a 10-line text block: verify the textarea caps at 5 lines and displays an internal vertical scrollbar without breaking the layout.
   - Press **Enter**: verify the textarea immediately resets to its compact 1-line height.

---

## 11. Step 10: Verify Conversation History, Settings & Privacy (Module C7)

1. **Test History Drawer & Multi-turn Continuity:**
   - In the header, click the **📜 History** button.
   - Observe the slide-over drawer opening from the left with a list of past conversations, ordered newest first.
   - Each item displays the conversation title, relative/short timestamp, message count badge, and a delete button (🗑️).
   - Click **+ New Chat**:
     - Verify active messages clear, `conversationId` is reset to null, and the drawer closes.
     - Send a new question (e.g., "Explain Merge Sort"). Verify a new conversation is created.
   - Re-open the history drawer and click the previous conversation:
     - Verify the prior conversation's messages reload chronologically.
     - Send a follow-up query; verify context continues in that same conversation.

2. **Test Conversation Deletion:**
   - In the history drawer, click the 🗑️ icon on an individual conversation.
   - Verify the conversation is removed from the drawer and backend database (cascade-deleting its messages).
   - If deleting the currently active conversation, verify the chat pane resets to empty state.

3. **Test Settings, Model Badge & Reasoning Level:**
   - In the header, click the **⚙️ Settings** icon.
   - Verify the modal displays:
     - Active AI model badge (e.g. `gemini-2.5-flash` or `gemini-3.5-flash-lite`).
     - Preferred Language selector (e.g. `Python`, `C#`, `TypeScript`).
     - AI Reasoning Level selector with options: `Low (Fastest response)`, `Medium (Balanced reasoning)`, `High (Deepest analysis)`.
     - Zero Code Persisted privacy badge.
   - Change Reasoning Level to `High` and save settings:
     - Verify success toast "Preferences saved successfully!".
     - Inspect `GET /api/preferences` to verify `reasoningLevel: "high"` and `model` are persisted.

4. **Test Clear All History & Data Minimization Verification:**
   - In the Settings modal under "Data Privacy & History", click **Clear All Conversation History**.
   - Observe the confirmation prompt appears: *"Are you sure? This cannot be undone."* with Cancel and Yes, Clear All buttons.
   - Click **Yes, Clear All**:
     - Verify all user conversations are removed.
     - Verify the chat stream resets to empty state.
   - Database Privacy Audit:
     - Query `SELECT [Content], [Metadata] FROM [Messages]` in SQL Server.
     - Verify no raw source code or problem text is stored in `Content` or `Metadata` (only metadata summaries: platform, host, title, language, code line count, isTruncated).


---

## 12. Step 11: Side Panel UI Polish & Definition of Done Checks

Follow these verification steps to manually audit the side panel design system, layout stability, and accessibility:

### Check 1: Composer Always Visible & Never Clipped (DoD #1)
1. **Window Height Testing (500px to 1200px):**
   - Resize Chrome browser window down to 500px height.
   - Verify the composer dock (input box + send button) remains fully visible with comfortable bottom padding (`12px + safe-area-inset`).
   - Resize Chrome browser window up to 1200px height; verify composer remains neatly docked at the bottom.
2. **Side Panel Width Testing (320px, 400px, 480px):**
   - Drag Chrome side panel boundary to minimum width (~320px).
   - Verify the context bar chips truncate cleanly into a single row with chevron toggle.
   - Verify the composer input box and send button do not overlap or wrap awkwardly.
   - Drag side panel width to 400px and 480px; verify fluid responsive expansion.
3. **Browser Zoom Testing (100%, 125%, 150%):**
   - In Chrome, press `Ctrl + +` to zoom to 125% and 150%.
   - Verify the composer never pushes below the viewport or requires vertical page scroll to reach.
4. **Stress Testing with Expanded Context & Error Banner:**
   - Click the context bar summary row to expand problem details.
   - Trigger a simulated error (e.g. rate limit error banner).
   - Observe the message stream shrinking gracefully while the composer stays anchored and accessible.

### Check 2: Rich Assistant Response & Zero Horizontal Page Scroll (DoD #2)
1. **Send Complex Markdown Query:**
   - In the composer, ask: *"Explain the optimal solution for Two Sum with headings, nested bullet lists, a comparison table, blockquote, inline code, a line of code over 100 characters, and a long URL."*
2. **Verify Rendering:**
   - **Headings**: H1, H2, and H3 render at readable chat scale (no oversized headings).
   - **Nested Lists**: Bullet points indent properly with clean hierarchy.
   - **Comparison Table**: Table renders inside `.table-wrapper` with horizontal scroll inside the wrapper; verify the main sidepanel window never develops horizontal scrollbars.
   - **Blockquote**: Renders with warm accent left border and desaturated background.
   - **Inline Code**: Clean dark chip with monospaced font and subtle border.
   - **100+ Character Code Line**: Scrolls horizontally INSIDE the code block with slim header (language tag on left, Copy button on right).
   - **Long URL**: Wraps cleanly without overflowing the container width.

### Check 3: Codexo Icon in Toolbar & Side Panel (DoD #3)
1. **Toolbar Icon:**
   - Look at the Chrome toolbar: verify the official Codexo icon (warm orange lightning bolt on dark container) appears crisply at 16px, 32px, and 48px sizes (no default puzzle piece or blank icon).
2. **Side Panel Header:**
   - Open the side panel: verify the top-left header displays the Codexo mark SVG next to the "Codexo" brand title.
3. **Build Artifacts:**
   - Inspect `extension\dist\codexo\icons`: confirm `icon-16.png`, `icon-32.png`, `icon-48.png`, `icon-128.png`, and `codexo-mark.svg` are present.

### Check 4: Zero-Flash Preload Theme (DoD #4)
1. **Open Side Panel:**
   - Close and re-open the side panel.
   - Observe first paint: the background paints immediately as dark `#191918` (or `#FAF9F5` in light mode) with zero white flash.
2. **CSP Inspection:**
   - Open Chrome DevTools for the side panel (`Inspect`).
   - Check the Console tab: verify zero Content Security Policy (CSP) errors or blocked script warnings.

### Check 5: Accessibility, Contrast & Touch Targets (DoD #5)
1. **Contrast Audit:**
   - Verify primary text (`#F3F1EA` on `#191918`) achieves 15.3:1 contrast.
   - Verify muted text (`#A8A59C` on `#191918`) achieves 7.0:1 contrast.
   - Verify active send button (`#191918` on `#D97757`) achieves 5.58:1 contrast.
   - In light mode (`prefers-color-scheme: light`), verify text contrast exceeds 5.1:1.
2. **Interactive Target Sizes:**
   - Inspect buttons (New Chat, History, Settings, Refresh, Send, Stop, Retry, Copy): verify each target is at least 32px by 32px.
3. **Keyboard Navigation & Visible Focus Rings:**
   - Press `Tab` repeatedly through all interactive controls.
   - Verify every focused control displays a clear orange focus ring (`outline: 2px solid var(--color-accent)`).
   - Verify screen reader accessible names via `aria-label` or `title` on every icon button.
