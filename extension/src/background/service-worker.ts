// Codexo Service Worker (MV3 Background Script)
// Coordinates lifecycle, side panel opening, and cross-context messaging bridge

import { MessageType, ExtensionMessage, ExtensionResponse, ProblemContextPayload } from "../shared/extension-messaging";

const tabProblems = new Map<number, ProblemContextPayload>();

// Open side panel when the extension toolbar action is clicked
chrome.action.onClicked.addListener((tab) => {
  if (tab.id) {
    chrome.sidePanel.open({ tabId: tab.id }).catch((err) => {
      console.error("[Codexo SW] Failed to open side panel:", err);
    });
  }
});

// Clean up cached state when tab is closed
chrome.tabs.onRemoved.addListener((tabId) => {
  tabProblems.delete(tabId);
});

// Messaging bridge router
chrome.runtime.onMessage.addListener(
  (
    message: ExtensionMessage,
    sender: chrome.runtime.MessageSender,
    sendResponse: (response: ExtensionResponse) => void
  ) => {
    switch (message.type) {
      case MessageType.Ping: {
        sendResponse({
          success: true,
          data: "PONG_FROM_SERVICE_WORKER",
        });
        return false;
      }

      case MessageType.ProblemDetected:
      case MessageType.ProblemUpdated: {
        const payload = message.payload as ProblemContextPayload | undefined;
        const tabId = sender.tab?.id;

        if (tabId && payload) {
          tabProblems.set(tabId, payload);
          console.log(`[Codexo SW] Cached problem for tab ${tabId}: ${payload.title} (${payload.platformProblemId})`);

          // Broadcast to side panel or other extension pages
          chrome.runtime.sendMessage({
            type: MessageType.ProblemUpdated,
            payload,
            timestamp: Date.now(),
          }).catch(() => {
            // Side panel may not be open yet; safe to ignore
          });
        }

        sendResponse({ success: true, data: payload });
        return false;
      }

      case MessageType.GetActiveProblem: {
        // Query the active tab in current window
        chrome.tabs.query({ active: true, currentWindow: true }).then(([activeTab]) => {
          if (activeTab?.id && tabProblems.has(activeTab.id)) {
            sendResponse({ success: true, data: tabProblems.get(activeTab.id) });
          } else {
            sendResponse({ success: true, data: null });
          }
        }).catch((err) => {
          sendResponse({ success: false, error: String(err) });
        });
        return true; // Keep message channel open for async query
      }

      default:
        return false;
    }
  }
);

console.log("[Codexo SW] Service worker initialized and messaging bridge active.");
