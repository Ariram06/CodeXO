// Codexo Service Worker (MV3 Background Script)
// Coordinates lifecycle, side panel opening, MAIN-world editor reading, and cross-context messaging bridge

import {
  MessageType,
  ExtensionMessage,
  ExtensionResponse,
  PageContextPayload,
} from '../shared/extension-messaging';
import { extractCodeFromMainWorld, EditorReadResult } from '../content/editor-reader';
import { redactSecrets } from '../shared/secret-redactor';

const tabContextCache = new Map<number, PageContextPayload>();

// Open side panel when the extension toolbar icon is clicked
chrome.action.onClicked.addListener((tab) => {
  if (tab.id) {
    chrome.sidePanel.open({ tabId: tab.id }).catch((err) => {
      console.error('[Codexo SW] Failed to open side panel:', err);
    });
  }
});

// Clean up cached state when tab is closed
chrome.tabs.onRemoved.addListener((tabId) => {
  tabContextCache.delete(tabId);
});

// Capture active tab context (MAIN-world editor extraction + content script DOM extraction)
export async function captureTabContext(tabId: number, tabUrl?: string, tabTitle?: string): Promise<PageContextPayload> {
  const url = tabUrl || '';
  const title = tabTitle || 'Untitled Page';

  let platform = 'Web';
  if (/leetcode\.(com|cn)/i.test(url)) platform = 'LeetCode';
  else if (/codeforces\.com/i.test(url)) platform = 'Codeforces';
  else if (/hackerrank\.com/i.test(url)) platform = 'HackerRank';
  else if (/geeksforgeeks\.org/i.test(url)) platform = 'GeeksforGeeks';
  else if (/codechef\.com/i.test(url)) platform = 'CodeChef';

  let code: string | undefined = undefined;
  let lineCount: number | undefined = undefined;
  let editorLanguage: string | undefined = undefined;

  // 1. If http(s) page, execute editor reading in MAIN world to reach Monaco / CodeMirror / Ace
  if (url.startsWith('http://') || url.startsWith('https://')) {
    try {
      const results = await chrome.scripting.executeScript({
        target: { tabId },
        world: 'MAIN',
        func: extractCodeFromMainWorld,
      });

      if (results && results[0]?.result) {
        const editorResult = results[0].result as EditorReadResult;
        if (editorResult.code) {
          code = redactSecrets(editorResult.code).slice(0, 20000);
          lineCount = editorResult.lineCount;
          editorLanguage = editorResult.language;
        }
      }
    } catch (err) {
      console.warn('[Codexo SW] MAIN world script execution failed or not permitted on tab:', err);
    }

    // 2. Query content script for problem text, error text, selection, and language
    try {
      const contentResponse = await chrome.tabs.sendMessage(tabId, {
        type: MessageType.GetPageContext,
      });

      if (contentResponse?.success && contentResponse.data) {
        const data = contentResponse.data as PageContextPayload;
        const merged: PageContextPayload = {
          ...data,
          platform: data.platform || platform,
          title: data.title || title,
          url,
          code: code ?? data.code,
          lineCount: lineCount ?? data.lineCount,
          language: data.language || editorLanguage,
        };
        tabContextCache.set(tabId, merged);
        return merged;
      }
    } catch {
      // Content script may not be loaded on generic site
    }
  }

  const fallback: PageContextPayload = {
    platform,
    url,
    title,
    code,
    lineCount,
    language: editorLanguage,
  };
  tabContextCache.set(tabId, fallback);
  return fallback;
}

// Tab switch listener: update context and broadcast to side panel
chrome.tabs.onActivated.addListener(async (activeInfo) => {
  try {
    const tab = await chrome.tabs.get(activeInfo.tabId);
    if (tab?.id) {
      const context = await captureTabContext(tab.id, tab.url, tab.title);
      chrome.runtime.sendMessage({
        type: MessageType.PageContextUpdated,
        payload: context,
        timestamp: Date.now(),
      }).catch(() => {});
    }
  } catch {
    // Tab might be closing
  }
});

// Tab navigation/update listener
chrome.tabs.onUpdated.addListener(async (tabId, changeInfo, tab) => {
  if (changeInfo.status === 'complete' && tab.active && tab.id) {
    try {
      const context = await captureTabContext(tabId, tab.url, tab.title);
      chrome.runtime.sendMessage({
        type: MessageType.PageContextUpdated,
        payload: context,
        timestamp: Date.now(),
      }).catch(() => {});
    } catch {
      // Tab might be closing
    }
  }
});

// Messaging router
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
          data: 'PONG_FROM_SERVICE_WORKER',
        });
        return false;
      }

      case MessageType.PageContextUpdated: {
        const payload = message.payload as PageContextPayload | undefined;
        const tabId = sender.tab?.id;
        if (tabId && payload) {
          tabContextCache.set(tabId, payload);
          // Broadcast to side panel
          chrome.runtime.sendMessage({
            type: MessageType.PageContextUpdated,
            payload,
            timestamp: Date.now(),
          }).catch(() => {});
        }
        sendResponse({ success: true, data: payload });
        return false;
      }

      case MessageType.GetPageContext: {
        chrome.tabs.query({ active: true, currentWindow: true }).then(async ([activeTab]) => {
          if (!activeTab?.id) {
            sendResponse({ success: true, data: null });
            return;
          }

          const context = await captureTabContext(activeTab.id, activeTab.url, activeTab.title);
          sendResponse({ success: true, data: context });
        }).catch((err) => {
          sendResponse({ success: false, error: String(err) });
        });
        return true; // Keep channel open for async response
      }

      case MessageType.RequestOptionalPermissions: {
        const origin = (message.payload as string) || 'https://*/*';
        chrome.permissions.request({ origins: [origin] }).then((granted) => {
          sendResponse({ success: true, data: granted });
        }).catch((err) => {
          sendResponse({ success: false, error: String(err) });
        });
        return true;
      }

      default:
        return false;
    }
  }
);

console.log('[Codexo SW] Service worker initialized with MAIN-world editor capture and tab event listeners.');
