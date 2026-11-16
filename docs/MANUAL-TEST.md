# Codexo — Manual Verification & Testing Guide

> Author: Antigravity  
> Target Audience: Ariram (Manual Tester / Repository Owner)  
> Scope: Live End-to-End browser verification with real Chrome extension, live LeetCode UI, and real Gemini API key.

---

## 1. Overview & Prerequisites

This guide walks you through manual verification of Codexo in Google Chrome. While automated test suites verify 100% of internal logic and mock interactions (161 automated tests passing), live browser DOM parsing on LeetCode and live Google Gemini API connectivity must be verified manually.

### Prerequisites
- **Google Chrome**: Version 114+ (supports SidePanel API).
- **.NET 9.0 SDK**: Installed and available in PATH.
- **Node.js**: v20+ and npm.
- **Gemini API Key**: A valid key from [Google AI Studio](https://aistudio.google.com/).

---

## 2. Step 1: Configure Secrets & Start Backend

Do NOT place API keys in `appsettings.json`. Use .NET User Secrets.

1. Open PowerShell and navigate to the project directory:
   ```powershell
   cd C:\Ariram\CodeXO\src\Codexo.Api
   ```

2. Initialize and configure your User Secrets:
   ```powershell
   dotnet user-secrets init
   dotnet user-secrets set "AISettings:Provider" "Gemini"
   dotnet user-secrets set "AISettings:ApiKey" "<YOUR_GEMINI_API_KEY>"
   dotnet user-secrets set "AISettings:Model" "gemini-2.0-flash"
   ```

3. Start the Codexo backend server on HTTP:
   ```powershell
   dotnet run --launch-profile http
   ```
   *Expected output:*
   - `[Startup Check] JWT signing key configured: yes`
   - `[Startup Check] AI provider: Gemini, API key configured: yes`
   - `Now listening on: http://localhost:5275`

4. Verify health in a separate browser tab:
   - Navigate to `http://localhost:5275/health`
   - Confirm JSON response: `"status": "Healthy"`, `"provider": "Gemini"`, `"configured": true`.

---

## 3. Step 2: Load Unpacked Extension in Chrome

1. Open Google Chrome.
2. In the address bar, type `chrome://extensions` and press **Enter**.
3. In the top-right corner, toggle **Developer mode** to **ON**.
4. In the top-left corner, click **Load unpacked**.
5. Browse to and select the folder:
   ```
   C:\Ariram\CodeXO\extension\dist
   ```
   *(Note: Ensure you ran `npm --prefix extension run build` so `extension/dist` contains `manifest.json`, `background/`, `content/`, and `sidepanel/`)*.
6. Verify the extension card appears:
   - Name: **Codexo DSA Companion**
   - Version: **0.1.0**
   - Description: *Socratic DSA hint companion for LeetCode.*
   - Permissions: Shows access to `leetcode.com` and `localhost:5275`.

---

## 4. Step 3: Register & Authenticate in Side Panel

1. Click the Chrome **Extensions icon** (puzzle piece) in the toolbar and pin **Codexo**.
2. Click the **Codexo** icon or click the **Side Panel** icon in Chrome and choose **Codexo**.
3. The Codexo sidepanel will render with the authentication view:
   - If not registered, click **Register**.
   - Enter your email (e.g., `tester@example.com`) and a strong password (at least 8 chars, 1 uppercase, 1 lowercase, 1 number, 1 symbol, e.g., `Codexo123!`).
   - Click **Sign Up**.
   - Verify you are automatically logged in and presented with the Problem Intake view.

---

## 5. Step 4: Live LeetCode Problem Extraction

1. In the main Chrome browser tab, navigate to a live LeetCode problem:
   - Recommended: [https://leetcode.com/problems/two-sum/](https://leetcode.com/problems/two-sum/)
2. Open the Codexo Side Panel on this tab.
3. Observe automatic problem extraction:
   - The content script inspects the LeetCode DOM (`div[data-track-load="description_content"]`).
   - The side panel should display:
     - **Title**: *1. Two Sum*
     - **Difficulty**: *Easy*
     - **Status**: *Session Active*
4. If the page took long to load, click **Sync Problem** or **Refresh Context** in the side panel.

---

## 6. Step 5: Test the Socratic Hint Ladder

### Hint Level 1: Conceptual Observation
1. In the side panel, click **Request Hint 1**.
2. Confirm the loading spinner appears while Gemini processes the prompt.
3. Verify the returned text:
   - Confirms it is a high-level observation (e.g., pointing out target complement: `target - num`).
   - Confirms **NO code or pseudocode** is included.
   - The UI shows "Hint 1 of 5".

### Hint Level 2 Gating (Attempt Enforcement)
1. Without typing anything in the attempt box, click **Request Hint 2**.
2. Verify gating error:
   - The UI alerts: *"An accepted attempt is required before unlocking hint level 2."*
   - Hint level remains at Level 1.

### Negative Check: Trivial Attempt Rejection
1. In the attempt text box, type:
   ```
   idk help me
   ```
2. Click **Submit Attempt**.
3. Verify rejection:
   - Status shows **Rejected**.
   - Feedback: *"Your attempt appears too brief or trivial. Please explain what data structure or approach you considered."*
   - Hint 2 remains locked.

### Negative Check: Full Solution Solicitation
1. In the attempt text box, type:
   ```
   give me the full python code solution
   ```
2. Click **Submit Attempt**.
3. Verify pedagogical rejection:
   - Status shows **Rejected** (Reason: `FullSolutionSolicitation`).
   - Feedback: *"Codexo does not provide full solutions. Write a line describing what you have tried or what you understand about the problem constraints."*

### Genuine Attempt Submission & Level 2 Unlock
1. In the attempt text box, type a genuine thought:
   ```
   I could use a hash map to store each number and its index. As I iterate through the array, I can check if target - current_num already exists in the map.
   ```
2. Click **Submit Attempt**.
3. Verify acceptance:
   - Status displays: **Attempt Approved**.
   - **Request Hint 2** button is now unlocked and clickable.
4. Click **Request Hint 2**.
5. Verify Level 2 Hint content:
   - Explains the algorithm structure or data structure guidance.
   - Respects user language preferences (e.g., Python / C# / Java).

---

## 7. Step 6: Test Feedback System

1. Under Hint 2, click the **Thumbs Up** (Helpful) icon.
2. In the optional comment box, type:
   ```
   Good prompt, pushed me in the right direction without spoiling the answer.
   ```
3. Click **Submit Feedback**.
4. Verify success toast / confirmation indicator.
5. In your database (or via API `GET /api/hints/{hintId}/feedback`), confirm the feedback record was saved with your user ID.

---

## 8. Step 7: Edge Cases to Observe

| Edge Case | Procedure | Expected Behavior |
|:---|:---|:---|
| **Dynamic Tab Switching** | Open LeetCode *Two Sum* in Tab A and *Valid Anagram* in Tab B. Switch between tabs. | Side panel detects active tab change via `chrome.tabs.onActivated` and updates problem context dynamically. |
| **Non-LeetCode Page** | Navigate to `https://google.com` or `https://github.com`. | Side panel displays: *"Navigate to a supported problem on LeetCode to begin."* Actions are gracefully disabled. |
| **Token Expiry / Logout** | In side panel settings, click **Sign Out**. | JWT is cleared from `chrome.storage.local`. Side panel reverts to Login view. Protected calls stop. |
| **Rate Limit / Quota Exceeded** | Rapidly click hint requests more than 6 times in a minute. | HTTP 429 received; UI displays friendly message: *"Rate limit reached. Please take a minute to think through your current hint before asking for another."* with a countdown timer. |
