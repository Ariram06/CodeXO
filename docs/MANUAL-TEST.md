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
