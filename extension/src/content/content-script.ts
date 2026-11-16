// Codexo Content Script (Injected on leetcode.com/problems/*)
// Extracts DSA problem metadata and syncs with Service Worker and Side Panel

import {
  MessageType,
  ExtensionMessage,
  ExtensionResponse,
  ProblemContextPayload,
} from '../shared/extension-messaging';
import { extractProblemFromDocument } from './leetcode-extractor';

let lastExtractedSlug: string | null = null;
let lastExtractedUrl: string = '';
let debounceTimer: ReturnType<typeof setTimeout> | null = null;

export function performExtraction(): ProblemContextPayload | null {
  const currentUrl = window.location.href;
  const problem = extractProblemFromDocument(document, currentUrl);

  if (problem) {
    if (problem.slug !== lastExtractedSlug || currentUrl !== lastExtractedUrl) {
      lastExtractedSlug = problem.slug;
      lastExtractedUrl = currentUrl;
      console.log(`[Codexo Content] Detected problem: "${problem.title}" (${problem.difficulty})`);

      try {
        chrome.runtime.sendMessage({
          type: MessageType.ProblemDetected,
          payload: problem,
          timestamp: Date.now(),
        });
      } catch (err) {
        console.warn('[Codexo Content] Failed to send ProblemDetected message:', err);
      }
    }
  }

  return problem;
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

// 2. MutationObserver to handle dynamically rendered SPA DOM elements
if (typeof MutationObserver !== 'undefined' && typeof document !== 'undefined') {
  const targetNode = document.body || document.documentElement;
  if (targetNode) {
    const observer = new MutationObserver((mutations) => {
      const hasAddedNodes = mutations.some((m) => m.type === 'childList' && m.addedNodes.length > 0);
      if (hasAddedNodes) {
        scheduleExtraction(500);
      }
    });

    observer.observe(targetNode, {
      childList: true,
      subtree: true,
    });
  }
}

// 3. Periodic URL polling to handle Next.js client-side route changes (pushState)
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

// 4. Listen for explicit extraction requests from Side Panel or Service Worker
if (typeof chrome !== 'undefined' && chrome.runtime?.onMessage) {
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

      if (message.type === MessageType.RequestProblemExtraction) {
        const problem = performExtraction();
        sendResponse({
          success: !!problem,
          data: problem ?? undefined,
          error: problem ? undefined : 'Problem could not be extracted from current page DOM.',
        });
        return false;
      }

      return false;
    }
  );
}
