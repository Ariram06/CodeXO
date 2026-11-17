// Codexo Content Script (Injected on supported coding platforms and sites)
// Coordinates problem text extraction, editor reading, selection capture, and secret sanitization

import {
  MessageType,
  ExtensionMessage,
  ExtensionResponse,
  PageContextPayload,
} from '../shared/extension-messaging';
import { extractPageContext } from './platforms.config';
import { extractCodeFromMainWorld } from './editor-reader';
import { redactSecrets } from '../shared/secret-redactor';

let lastExtractedUrl: string = '';
let debounceTimer: ReturnType<typeof setTimeout> | null = null;

export function performExtraction(): PageContextPayload {
  const currentUrl = window.location.href;
  const pageData = extractPageContext(document, currentUrl);

  // Attempt local editor extraction (isolated world fallback)
  const editorResult = extractCodeFromMainWorld(window);
  const code = editorResult.code ? redactSecrets(editorResult.code).slice(0, 20000) : undefined;

  const payload: PageContextPayload = {
    platform: pageData.platform,
    url: currentUrl,
    title: pageData.title,
    problemText: pageData.problemText,
    language: pageData.language || editorResult.language,
    code,
    lineCount: editorResult.lineCount,
    selection: pageData.selection,
    errorText: pageData.errorText,
  };

  if (currentUrl !== lastExtractedUrl) {
    lastExtractedUrl = currentUrl;
    console.log(`[Codexo Content] Context extracted on ${payload.platform}: "${payload.title}"`);

    try {
      chrome.runtime.sendMessage({
        type: MessageType.PageContextUpdated,
        payload,
        timestamp: Date.now(),
      });
    } catch {
      // Extension context invalidated or sidepanel not open yet
    }
  }

  return payload;
}

export function scheduleExtraction(delayMs = 400): void {
  if (debounceTimer) {
    clearTimeout(debounceTimer);
  }
  debounceTimer = setTimeout(() => {
    performExtraction();
  }, delayMs);
}

// 1. Initial extraction attempt
scheduleExtraction(200);

// 2. MutationObserver for dynamic single-page applications (LeetCode, HackerRank SPA transitions)
if (typeof MutationObserver !== 'undefined' && typeof document !== 'undefined') {
  const targetNode = document.body || document.documentElement;
  if (targetNode) {
    const observer = new MutationObserver((mutations) => {
      const hasAddedNodes = mutations.some((m) => m.type === 'childList' && m.addedNodes.length > 0);
      if (hasAddedNodes) {
        scheduleExtraction(600);
      }
    });

    observer.observe(targetNode, {
      childList: true,
      subtree: true,
    });
  }
}

// 3. Periodic URL polling for pushState SPA routing
if (typeof window !== 'undefined') {
  let currentHref = window.location.href;
  setInterval(() => {
    if (window.location.href !== currentHref) {
      currentHref = window.location.href;
      scheduleExtraction(300);
    }
  }, 1000);

  window.addEventListener('popstate', () => {
    currentHref = window.location.href;
    scheduleExtraction(300);
  });
}

// 4. Message Listener for queries from Side Panel or Service Worker
if (typeof chrome !== 'undefined' && chrome.runtime?.onMessage) {
  chrome.runtime.onMessage.addListener(
    (
      message: ExtensionMessage,
      _sender: chrome.runtime.MessageSender,
      sendResponse: (response: ExtensionResponse) => void
    ) => {
      if (message.type === MessageType.Ping) {
        sendResponse({
          success: true,
          data: `PONG_FROM_CONTENT_SCRIPT_${window.location.pathname}`,
        });
        return false;
      }

      if (
        message.type === MessageType.GetPageContext ||
        message.type === MessageType.RequestProblemExtraction
      ) {
        const payload = performExtraction();
        sendResponse({
          success: true,
          data: payload,
        });
        return false;
      }

      return false;
    }
  );
}
