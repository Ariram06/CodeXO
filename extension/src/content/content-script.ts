// Codexo Content Script (Injected on leetcode.com/problems/*)
// Bridges webpage DOM context to background service worker and side panel

import { MessageType, ExtensionMessage, ExtensionResponse } from "../shared/extension-messaging";

console.log("[Codexo Content] Initializing content script messaging bridge on", window.location.href);

// Diagnostic Ping to verify messaging bridge with Service Worker
try {
  chrome.runtime.sendMessage(
    {
      type: MessageType.Ping,
      payload: "CONTENT_SCRIPT_INITIALIZED",
      timestamp: Date.now(),
    },
    (response: ExtensionResponse) => {
      if (chrome.runtime.lastError) {
        console.warn("[Codexo Content] Service worker not yet awake:", chrome.runtime.lastError.message);
      } else {
        console.log("[Codexo Content] Received bridge pong from SW:", response?.data);
      }
    }
  );
} catch (err) {
  console.warn("[Codexo Content] Message dispatch skipped:", err);
}

// Listen for messages from Side Panel or Service Worker
chrome.runtime.onMessage.addListener(
  (
    message: ExtensionMessage,
    sender: chrome.runtime.MessageSender,
    sendResponse: (response: ExtensionResponse) => void
  ) => {
    if (message.type === MessageType.Ping) {
      sendResponse({
        success: true,
        data: `PONG_FROM_CONTENT_SCRIPT_${window.location.pathname}`,
      });
      return false;
    }

    return false;
  }
);
